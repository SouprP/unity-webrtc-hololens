using System;
using System.Collections;
using System.Collections.Generic;
using Internal;
using Models;
using Unity.WebRTC;
using UnityEngine;

public class WebRTC_Manager : MonoBehaviour
{
    private WebSocketConnection _ws;
    private RTCPeerConnection _peer;
    private RTCDataChannel _serverChannel;
    private Dictionary<string, RTCDataChannel> _dataChannels = new Dictionary<string, RTCDataChannel>();

    private bool _hasReceiverOffer = false;
    // private SessionDescription _receivedOfferSessionDescTemp;

    [SerializeField] private string server = "localhost";
    [SerializeField] private uint port = 8080;
    
    async void Awake()
    {
        _ws = new WebSocketConnection();
        Debug.Log("Awake");

        if (port > 0)
        {
            await _ws.ConnectAsync(server, port);
            _ws.OnMessageReceived += OnWebSocketMessage;
            _ws.OnConnectionOpened += OnWebSocketOpen;
            // Debug.Log("Awake port");
            return;
        }

        await _ws.ConnectAsync(server);
        _ws.OnMessageReceived += OnWebSocketMessage;
        _ws.OnConnectionOpened += OnWebSocketOpen;
        // Debug.Log("Awake default");
    }

    async void OnDestroy()
    {
        try
        {
            _serverChannel.Close();
            foreach (var obj in _dataChannels)
                obj.Value.Close();

            await _ws.DisconnectAsync();
        }
        catch (Exception ignored)
        {
            
        }
    }

    /**
     *
     *          PRIVATE
     *
     */

    private void InitClient()
    {
        _peer = new RTCPeerConnection();
        Debug.Log("Init");
        // _peer.OnIceCandidate = candidate =>
        // {
        //     var candidateInit = new CandidateInit()
        //     {
        //         SdpMid = candidate.SdpMid,
        //         SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
        //         Candidate = candidate.Candidate
        //     };
        //     _ws.SendAsync(MessageTypes.CANDIDATE.ToString() + candidateInit);
        // };
        //
        // _peer.OnIceConnectionChange = state =>
        // {
        //     Debug.Log(state);
        // };
        //
        // _peer.OnDataChannel = channel =>
        // {
        //     _serverChannel = channel;
        //     _serverChannel.OnMessage = bytes =>
        //     {
        //         var message = System.Text.Encoding.UTF8.GetString(bytes);
        //         Debug.Log("Receiver received: " + message);
        //     };
        // };
    }

    private void OnWebSocketOpen()
    {
        Debug.Log("Opened!");
    }

    private void OnWebSocketMessage(string message)
    {
        Message baseMessage = JsonUtility.FromJson<Message>(message);
        Debug.Log(message);

        switch (baseMessage.type.ToLower())
        {
            case "join_session":
                var joinSessionMessage = JsonUtility.FromJson<JoinSessionMessage>(message);
                HandleJoinSession(joinSessionMessage);
                break;

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
    
        private void HandleJoinSession(JoinSessionMessage message)
    {
        Debug.Log("Handling Join Session with session ID: " + message.session_id);
    }

    private void HandleJoinedSuccessfully(JoinedSuccessfullyMessage message)
    {
        Debug.Log("Joined successfully, number of peers: " + message.peer_amount);
    }

    private void HandleSdpOffer(SdpOfferMessage message)
    {
        Debug.Log("Handling SDP Offer from peer: " + message.peer_id);
        InitClient();

        var offerDesc = new RTCSessionDescription
        {
            sdp = message.sdp,
            type = RTCSdpType.Offer
        };
    
        // Set the remote description with the received SDP offer
        _peer.SetRemoteDescription(ref offerDesc);
    
        // Create an answer to the offer
        var answerDesc = _peer.CreateAnswer().Desc;
    
        // Set the local description with the generated SDP answer
        _peer.SetLocalDescription(ref answerDesc);
    
        // Send the answer back to the peer
        var answerMessage = new SdpAnswerMessage
        {
            type = "sdp_answer",
            peer_id = message.peer_id,
            sdp = answerDesc.sdp
        };
    
        _ws.SendAsync(JsonUtility.ToJson(answerMessage));
    }

    private void HandleSdpAnswer(SdpAnswerMessage message)
    {
        Debug.Log("Handling SDP Answer from peer: " + message.peer_id);
        var answerDesc = new RTCSessionDescription
        {
            sdp = message.sdp,
            type = RTCSdpType.Answer
        };
        _peer.SetRemoteDescription(ref answerDesc);
    }

    private void HandleCandidate(CandidateMessage message)
    {
        Debug.Log("Handling ICE Candidate from peer: " + message.peer_id);
        var candidate = new RTCIceCandidate(new RTCIceCandidateInit
        {
            candidate = message.candidate,
            sdpMid = null,
            sdpMLineIndex = 0 
        });
        _peer.AddIceCandidate(candidate);
    }

    private void HandleOpenDataChannel(OpenDataChannelMessage message)
    {
        Debug.Log("Handling Open Data Channel with SDP: " + message.sdp);
        CreateDataChannel("serverChannel");
    }

    private void CreateDataChannel(string label)
    {
        var dataChannelOptions = new RTCDataChannelInit();
        var dataChannel = _peer.CreateDataChannel(label, dataChannelOptions);
        _dataChannels[label] = dataChannel;

        dataChannel.OnOpen = () => Debug.Log($"{label} data channel opened.");
        dataChannel.OnClose = () => Debug.Log($"{label} data channel closed.");
        dataChannel.OnMessage = bytes =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Message received on {label}: " + message);
        };
    }
}
