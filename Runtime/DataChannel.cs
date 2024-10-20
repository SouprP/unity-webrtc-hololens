using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

public class DataChannel : MonoBehaviour
{
    private RTCPeerConnection _peer;
    private RTCDataChannel _dataChannel;

    private WebSocketConnection _ws;
    private string _clientId;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    
}
