using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Models;
using Unity.WebRTC;
using UnityEngine;

public class WebRTCClient 
{
    private WebSocketConnection _ws;
    private RTCPeerConnection _peer;
    private RTCDataChannel _serverChannel;
    private Dictionary<string, RTCDataChannel> _dataChannels = new Dictionary<string, RTCDataChannel>();
    
    private bool _isChannelOpen = false;
    private bool _debug = false;

    private string _server = "localhost"; 
    private uint _port = 8765;
    private string _sessionId = "session1";
    private string _peerId = "peer1";

    /**
     *
     *          PUBLIC API
     *
     */


    public WebRTCClient()
    {
        // this.sessionId = Guid.NewGuid().ToString();
        this._peerId = Guid.NewGuid().ToString();
    }
    public WebRTCClient(string server, uint port)
    {
        // this.sessionId = Guid.NewGuid().ToString();
        this._peerId = Guid.NewGuid().ToString();
        this._server = server;
        this._port = port;
    }

    public WebRTCClient(string sessionId)
    {
        this._sessionId = sessionId;
        this._peerId = Guid.NewGuid().ToString();
        this._peerId = "sfu_" + Guid.NewGuid().ToString();
    }
    public WebRTCClient(string sessionId, string peerId)
    {
        this._sessionId = sessionId;
        this._peerId = peerId;
    }
    public WebRTCClient(string server, uint port, string sessionId, string peerId)
    {
        this._server = server;
        this._port = port;
        this._sessionId = sessionId;
        this._peerId = peerId;
    }
    
    public void SetServer(string server){this._server = server;}
    public void SetPort(uint port){this._port = port;}
    public void SetSessionId(string sessionId){this._sessionId = sessionId;}
    public void SetPeerId(string peerId){this._peerId = peerId;}

    public void EnableDebug(bool enabled)
    {
        this._debug = enabled;
    }

    public bool IsChannelOpen()
    {
        return _isChannelOpen;
    }
    
    public async void InitClient()
    {
        _ws = new WebSocketConnection();
        
        // try connecting to a websocket
        int tries = 0;
        while (_ws.getState() != WebSocketState.Open)
        {
            if (_port > 0)
            {
                await _ws.ConnectAsync(_server, _port);
            }
            else
            {
                await _ws.ConnectAsync(_server);
            }

            // after 5 tries throw an exception
            if (tries == 5)
                throw new Exception("Could not establish connection to server");

            tries++;
        }
        
        // websocket event handlers
        _ws.OnConnectionOpened += OnWebSocketOpen;
        _ws.OnMessageReceived += OnWebSocketMessage;
        
        // peer connection setup;
        _peer = new RTCPeerConnection();
        
        // peer event handlers
        // _peer.OnIceCandidate += OnIceCandidate;
        _peer.OnDataChannel += OnDataChannel;
        
        _peer.SetLocalDescription();

        // sending a join request to the SFU
        var joinJson = new JoinSessionMessage()
        {
            type = "join_session",
            session_id = _sessionId,
            peer_id = _peerId,
        };
        
        await _ws.SendAsync(JsonUtility.ToJson(joinJson));
    }

    public async void Leave()
    {
        var leaveJson = new LeaveSessionMessage()
        {
            type = "leave_session",
            session_id = _sessionId,
            peer_id = _peerId,
        };
        
        await _ws.SendAsync(JsonUtility.ToJson(leaveJson));
    }

    public void Send(string data)
    {
        _serverChannel.Send(data);
    }

    public void Send(byte[] data)
    {
        _serverChannel.Send(data);
    }
    
    
    /**
     *
     *
     *          PRIVATE API
     * 
     */
    private void OnWebSocketMessage(string message)
    {
        Message baseMessage = JsonUtility.FromJson<Message>(message);
        if (_debug)
            Debug.Log("Received: " + baseMessage.type);

        switch (baseMessage.type.ToLower())
        {
            // case "join_session":
            //     var joinSessionMessage = JsonUtility.FromJson<JoinSessionMessage>(message);
            // HandleJoinSession(joinSessionMessage);
            // break;

            case "joined_successfully":
                var joinedSuccessfullyMessage = JsonUtility.FromJson<JoinedSuccessfullyMessage>(message);
                HandleJoinedSuccessfully(joinedSuccessfullyMessage);
                break;

            case "sdp_offer":
                var sdpOfferMessage = JsonUtility.FromJson<SdpOfferMessage>(message);
                HandleSdpOffer(sdpOfferMessage);
                break;

            case "sdp_answer":
                var sdpAnswerMessage = JsonUtility.FromJson<SdpAnswerMessage>(message);
                HandleSdpAnswer(sdpAnswerMessage);
                break;

            case "candidate":
                var candidateMessage = JsonUtility.FromJson<CandidateMessage>(message);
                HandleCandidate(candidateMessage);
                break;

            case "open_data_channel":
                var openDataChannelMessage = JsonUtility.FromJson<OpenDataChannelMessage>(message);
                HandleOpenDataChannel(openDataChannelMessage);
                break;
            
            default:
                Debug.LogWarning("Unhandled message type: " + baseMessage.type);
                break;
        }
        
    }
    
    private void OnWebSocketOpen()
    {
        if (_debug)
            Debug.Log("Opened!");
    }

    /**
     *
     *
     *          HANDLERS
     *
     */

    private async void HandleJoinedSuccessfully(JoinedSuccessfullyMessage message)
    {
        // setup and sending sdp offer to server
        var sdpJson = new SdpOfferMessage()
        {
            type = "sdp_offer",
            session_id = _sessionId,
            peer_id = _peerId,
            sdp = _peer.LocalDescription.sdp
        };
        
        await _ws.SendAsync(JsonUtility.ToJson(sdpJson));
    }

    private async void HandleSdpOffer(SdpOfferMessage message)
    {
        if (_debug)
            Debug.Log("Handling SDP Offer from peer: " + message.peer_id);
        
        Debug.Log("SDP OFFER sdp: " + message.sdp);
        var offerDesc = new RTCSessionDescription()
        {
            type = RTCSdpType.Offer,
            sdp = message.sdp,
        };

        // setting the remote description with the receiver sdp offer
        _peer.SetRemoteDescription(ref offerDesc);

        // creating and sending sdp anwser
        var answer = _peer.CreateAnswer();
        var answerDesc = answer.Desc;
        Debug.Log("Desc SDP: " + answerDesc.sdp);
        _peer.SetLocalDescription(ref answerDesc);
        
        var answerMessage = new SdpAnswerMessage
        {
            type = "sdp_answer",
            peer_id = message.peer_id,
            sdp = answerDesc.sdp
        };
        
        await _ws.SendAsync(JsonUtility.ToJson(answerMessage));
        
        if (_debug)
            Debug.Log("SDP Answer sent successfully!");
        
        // THIS WORKS
        // var channelJson = new OpenDataChannelMessage()
        // {
        //     type = "open_data_channel",
        //     session_id = _sessionId,
        //     channel_id = "channel_data_1",
        //     peer_id = this._peerId,
        // };
        //
        // Debug.Log(JsonUtility.ToJson(channelJson));
        //
        // await _ws.SendAsync(JsonUtility.ToJson(channelJson));
    }

    private async void HandleSdpAnswer(SdpAnswerMessage message)
    {
        if(_debug)
            Debug.Log("Handling SDP Answer from peer: " + message.peer_id);
        
        var answerDesc = new RTCSessionDescription
        {
            sdp = message.sdp,
            type = RTCSdpType.Answer
        };
        _peer.SetRemoteDescription(ref answerDesc);
        

        // var channelJson = new OpenDataChannelMessage()
        // {
        //     type = "open_data_channel",
        //     session_id = _sessionId,
        //     channel_id = "channel_data_1",
        //     peer_id = this._peerId,
        // };
        //
        // Debug.Log(JsonUtility.ToJson(channelJson));
        //
        // await _ws.SendAsync(JsonUtility.ToJson(channelJson));
    }
    
    private void HandleCandidate(CandidateMessage message)
    {
        if (_debug)
            Debug.Log($"Received ICE candidate from SFU: {message.candidate}");

        var candidateInit = new RTCIceCandidateInit
        {
            candidate = message.candidate["candidate"],
            sdpMid = message.candidate["sdpMid"],
            sdpMLineIndex = int.Parse(message.candidate["sdpMLineIndex"]),
        };

        var iceCandidate = new RTCIceCandidate(candidateInit);
        _peer.AddIceCandidate(iceCandidate);
    }
    
    private void OnDataChannel(RTCDataChannel channel)
    {
        if (_debug) Debug.Log("Data channel opened: " + channel.Label);

        _dataChannels[channel.Label] = channel;
        channel.OnMessage = bytes => Debug.Log($"Received message on {channel.Label}: {System.Text.Encoding.UTF8.GetString(bytes)}");
    }

    private void HandleOpenDataChannel(OpenDataChannelMessage message)
    {
        if (_debug) 
            Debug.Log("Opening data channel: " + message.channel_id);
        
        var dataChannel = _peer.CreateDataChannel(message.channel_id);
        _dataChannels[message.channel_id] = dataChannel;
        _serverChannel = dataChannel;
        dataChannel.OnOpen = () =>
        {
            Debug.Log($"Data channel {message.channel_id} opened.");
            _isChannelOpen = true;
        };
        dataChannel.OnMessage = bytes => Debug.Log($"Received message on {message.channel_id}: {System.Text.Encoding.UTF8.GetString(bytes)}");

        
    }

    private void OnChannelMessage()
    {
        
    }
}
