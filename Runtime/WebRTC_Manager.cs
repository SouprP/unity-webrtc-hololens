using System;
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

public class WebRTC_Manager : MonoBehaviour
{
    private WebSocketConnection _ws;
    private RTCPeerConnection _peer;
    private RTCDataChannel _serverChannel;
    private Dictionary<string, RTCDataChannel> _dataChannels = new Dictionary<string, RTCDataChannel>();

    private bool _hasReceiverOffer = false;
    private SessionDescription _receivedOfferSessionDescTemp;

    [SerializeField] private string server = "localhost";
    [SerializeField] private uint port = 0;
    
    async void Awake()
    {
        _ws = new WebSocketConnection();

        if (port != 0)
        {
            await _ws.ConnectAsync(server, port);
            return;
        }

        await _ws.ConnectAsync(server);
    }

    async void OnDestroy()
    {
        _serverChannel.Close();
        foreach (var obj in _dataChannels)
            obj.Value.Close();

        await _ws.DisconnectAsync();
    }

    /**
     *
     *          PRIVATE
     *
     */

    private void InitClient(object sender)
    {
        _peer = new RTCPeerConnection();
        _peer.OnIceCandidate = candidate =>
        {
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };
            _ws.SendAsync("CANDIDATE" + candidateInit);
        };

        _peer.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };

        _peer.OnDataChannel = channel =>
        {
            _serverChannel = channel;
            _serverChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Receiver received: " + message);
            };
        };

    }
}
