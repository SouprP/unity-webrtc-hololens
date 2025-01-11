using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC;
using UnityEngine;
using Models;

public class WebRTCClient
{
    private WebSocketConnection _ws;
    private PeerConnection _peer;
    private DataChannel _serverChannel;

    public delegate void OnDataReceivedDelegate(byte[] data);
    public event OnDataReceivedDelegate OnDataReceived;

    private bool _isChannelOpen = false;
    private bool _debug = false;

    private string _server = "localhost";
    private uint _port = 8765;
    private string _sessionId = "S1";
    private string _peerId = "unity_client_p1";
    private string _channelId = "chat";

    public WebRTCClient() { }

    public WebRTCClient(string server, uint port)
    {
        _peerId = Guid.NewGuid().ToString();
        _server = server;
        _port = port;
    }

    public void SetServer(string server) => _server = server;
    public void SetPort(uint port) => _port = port;
    public void SetSessionId(string sessionId) => _sessionId = sessionId;
    public void SetPeerId(string peerId) => _peerId = peerId;
    public void SetChannel(string channelId) => _channelId = channelId;

    public void EnableDebug(bool enabled) => _debug = enabled;

    public bool IsChannelOpen() => _isChannelOpen;

    public async void InitClient()
    {
        _ws = new WebSocketConnection();

        // Connect to WebSocket
        int tries = 0;
        while (_ws.getState() != System.Net.WebSockets.WebSocketState.Open)
        {
            if (_debug) Debug.Log($"Attempting WebSocket connection to ws://{_server}:{_port}");
            await _ws.ConnectAsync(_server, _port);
            if (++tries == 5)
                throw new Exception("Could not establish connection to server");
        }

        _ws.OnConnectionOpened += OnWebSocketOpen;
        _ws.OnMessageReceived += OnWebSocketMessage;

        // Set up the PeerConnection
        _peer = new PeerConnection();
        await _peer.InitializeAsync(new PeerConnectionConfiguration());
        
        _peer.DataChannelAdded += OnDataChannelAdded;

        _serverChannel = await _peer.AddDataChannelAsync(_channelId, true, true);
        _serverChannel.StateChanged += OnDataChannelStateChanged;
        _serverChannel.MessageReceived += OnChannelMessage;
        
        // Fix later, not needed right now
        // _peerConnection.IceCandidateReadytoSend += iceCandidate =>
        // {
        //     if (_debug) Debug.Log($"Local ICE candidate ready: {iceCandidate}");
        //
        //     var iceCandidateMessage = new CandidateMessage
        //     {
        //         type = "candidate",
        //         session_id = _sessionId,
        //         peer_id = _peerId,
        //         candidate = new Dictionary<string, string>
        //         {
        //             { "candidate", iceCandidate.Content },
        //             { "sdpMLineIndex", iceCandidate.SdpMlineIndex.ToString() },
        //             { "sdpMid", iceCandidate.SdpMid }
        //         }
        //     };
        //
        //     _ws.SendAsync(JsonUtility.ToJson(iceCandidateMessage));
        //     if (_debug) Debug.Log("ICE candidate sent to signaling server.");
        // };

        // Send a join request to the SFU
        var joinJson = new JoinSessionMessage
        {
            type = "join_session",
            session_id = _sessionId,
            peer_id = _peerId,
        };
        await _ws.SendAsync(JsonUtility.ToJson(joinJson));
    }

    public async void Close()
    {
        // Notify the server about leaving the session
        var leaveJson = new LeaveSessionMessage
        {
            type = "leave_session",
            session_id = _sessionId,
            peer_id = _peerId,
        };
        await _ws.SendAsync(JsonUtility.ToJson(leaveJson));

        _peer?.Close();
        _peer = null;
    }

    public void Send(string data)
    {
        if (_serverChannel.State == DataChannel.ChannelState.Open)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            _serverChannel.SendMessage(buffer);
        }
    }

    public void Send(byte[] data)
    {
        if (_serverChannel.State == DataChannel.ChannelState.Open)
        {
            _serverChannel.SendMessage(data);
        }
    }

    private void HandleJoinedSuccessfully(JoinedSuccessfullyMessage message)
    {
        if (_debug) Debug.Log("Joined session successfully, creating SDP offer...");

        // LocalSdpReadytoSend event handler
        _peer.LocalSdpReadytoSend += async (SdpMessage sdp) =>
        {
            if (_debug) Debug.Log($"Local SDP ready to send: {sdp.Type}");

            var sdpOfferJson = new SdpOfferMessage()
            {
                type = "sdp_offer",
                session_id = _sessionId,
                peer_id = _peerId,
                sdp = sdp.Content,
            };

            await _ws.SendAsync(JsonUtility.ToJson(sdpOfferJson));
            if (_debug) Debug.Log("SDP offer sent to signaling server.");
        };

        // Create the SDP offer
        _peer.CreateOffer();
    }

    private async void HandleSdpAnswer(SdpAnswerMessage message)
    {
        if (_debug) Debug.Log($"Handling SDP Answer from peer: {message.peer_id}");
        
        // Get the SDP Answer from receiver message
        var answer = new SdpMessage
        {
            Type = SdpMessageType.Answer,
            Content = message.sdp
        };
        await _peer.SetRemoteDescriptionAsync(answer);
    }

    private void HandleCandidate(CandidateMessage message)
    {
        // ICE is not used right now
        return;
        
        if (_debug) Debug.Log($"Received ICE candidate: {message.candidate}");

        // Parse and add the received ICE candidate to the PeerConnection
        var iceCandidate = new IceCandidate
        {
            SdpMid = message.candidate["sdpMid"],
            SdpMlineIndex = int.Parse(message.candidate["sdpMLineIndex"]),
            Content = message.candidate["candidate"]
        };

        _peer.AddIceCandidate(iceCandidate);
    }

    private void OnChannelMessage(byte[] data)
    {
        OnDataReceived?.Invoke(data);
    }
    
    private void OnWebSocketMessage(string message)
    {
        var baseMessage = JsonUtility.FromJson<Message>(message);
        if (_debug) Debug.Log($"Received: {baseMessage.type}");

        switch (baseMessage.type.ToLower())
        {
            case "joined_successfully":
                var joinedMessage = JsonUtility.FromJson<JoinedSuccessfullyMessage>(message);
                HandleJoinedSuccessfully(joinedMessage);
                break;

            case "sdp_answer":
                var sdpAnswerMessage = JsonUtility.FromJson<SdpAnswerMessage>(message);
                HandleSdpAnswer(sdpAnswerMessage);
                break;

            // ICE is not implemented
            case "candidate":
                var candidateMessage = JsonUtility.FromJson<CandidateMessage>(message);
                HandleCandidate(candidateMessage);
                break;
            
            // do nothing
            case "peer_joined":
                break;

            // notify about error / unrecognized message
            default:
                Debug.LogWarning($"Unhandled message type: {baseMessage.type}");
                break;
        }
    }

    private void OnWebSocketOpen()
    {
        if (_debug) Debug.Log("WebSocket connection opened.");
    }

    private void OnDataChannelAdded(DataChannel dataChannel)
    {
        if (_debug) Debug.Log("Data channel opened.");
        _serverChannel = dataChannel;
    }

    private void OnDataChannelStateChanged()
    {
        _isChannelOpen = _serverChannel.State == DataChannel.ChannelState.Open;
        if (_debug) Debug.Log($"Data channel state changed to: {_serverChannel.State}");
    }
}
