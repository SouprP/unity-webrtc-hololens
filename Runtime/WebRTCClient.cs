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
    
    public delegate void OnDataReceivedDelegate(byte[] data);
    public event OnDataReceivedDelegate OnDataReceived;
    
    private bool _isChannelOpen = false;
    private bool _debug = false;

   [SerializeField] private string _server = "localhost"; 
   [SerializeField] private uint _port = 8765;
   [SerializeField] private string _sessionId = "S1";
   [SerializeField] private string _peerId = "unity_client_p1";
   [SerializeField] private string _channelId = "chat";

    /**
     *
     *          PUBLIC API
     *
     */


    public WebRTCClient()
    {
        
    }
    public WebRTCClient(string server, uint port)
    {
        this._peerId = Guid.NewGuid().ToString();
        this._server = server;
        this._port = port;
    }
    public WebRTCClient(string sessionId)
    {
        this._sessionId = sessionId;
        this._peerId = Guid.NewGuid().ToString();
        // this._peerId = "sfu_" + Guid.NewGuid().ToString();
    }
    public WebRTCClient(string sessionId, string peerId)
    {
        this._sessionId = sessionId;
        this._peerId = peerId;
    }
    public WebRTCClient(string sessionId, string peerId, string channelId)
    {
        this._sessionId = sessionId;
        this._peerId = peerId;
        this._channelId = channelId;
    }
    public WebRTCClient(string server, uint port, string sessionId, string peerId)
    {
        this._server = server;
        this._port = port;
        this._sessionId = sessionId;
        this._peerId = peerId;
    }
    public WebRTCClient(string server, uint port, string sessionId, string peerId, string channelId)
    {
        this._server = server;
        this._port = port;
        this._sessionId = sessionId;
        this._peerId = peerId;
        this._channelId = channelId;
    }
    
    public void SetServer(string server){this._server = server;}
    public void SetPort(uint port){this._port = port;}
    public void SetSessionId(string sessionId){this._sessionId = sessionId;}
    public void SetPeerId(string peerId){this._peerId = peerId;}
    public void SetChannel(string channelId)
    {
        this._channelId = channelId;
    }

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
            if(_debug) Debug.Log("Attempting connection to websocket ws://" + this._server + ":" + this._port);
                
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
        // _peer.OnDataChannel += OnDataChannel;
        _serverChannel = _peer.CreateDataChannel(_channelId);
        _serverChannel.OnOpen += OnDataChannel;
        // _peer.OnIceConnectionChange = state => Debug.Log("Ice connection changed to " + state); 
        _serverChannel.OnMessage += OnChannelMessage;

        // sending a join request to the SFU
        var joinJson = new JoinSessionMessage()
        {
            type = "join_session",
            session_id = _sessionId,
            peer_id = _peerId,
        };
        
        await _ws.SendAsync(JsonUtility.ToJson(joinJson));
    }

    public async void Close()
    {
        _serverChannel.Close();
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

            case "joined_successfully":
                var joinedSuccessfullyMessage = JsonUtility.FromJson<JoinedSuccessfullyMessage>(message);
                HandleJoinedSuccessfully(joinedSuccessfullyMessage);
                break;

            case "sdp_answer":
                var sdpAnswerMessage = JsonUtility.FromJson<SdpAnswerMessage>(message);
                HandleSdpAnswer(sdpAnswerMessage);
                break;

            case "candidate":
                var candidateMessage = JsonUtility.FromJson<CandidateMessage>(message);
                HandleCandidate(candidateMessage);
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
        _peer.SetLocalDescription();
        var sdpJson = new SdpOfferMessage()
        {
            type = "sdp_offer",
            session_id = _sessionId,
            peer_id = _peerId,
            sdp = _peer.LocalDescription.sdp
        };
        
        await _ws.SendAsync(JsonUtility.ToJson(sdpJson));
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
        _isChannelOpen = true;
        
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
    
    private void OnDataChannel()
    {
        if(_debug) Debug.Log("Channel opened");

        // _serverChannel.Send("Hello World!");
    }

    /**
     *
     *
     *          CUSTOM EVENTS
     *
     */
    private void OnChannelMessage(byte[] data)
    {
        OnDataReceived?.Invoke(data);
    }
}
