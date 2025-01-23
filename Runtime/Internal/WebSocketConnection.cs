using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum ConnectionState
{
    Open,
    Closed,
    Connected,
    Error,
    None,
}
public class WebSocketConnection
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;

    private ConnectionState _state;
    
    
    public delegate void OnConnectionOpenedHandler();
    public delegate void OnConnectionClosedHandler();
    public delegate void OnMessageReceivedHandler(string message);
    public delegate void OnErrorHandler(Exception ex);
    
    public event OnConnectionOpenedHandler OnConnectionOpened;
    public event OnConnectionClosedHandler OnConnectionClosed;
    public event OnMessageReceivedHandler OnMessageReceived;
    public event OnErrorHandler OnError;

    private const int _bufferSize = 1024;

    public WebSocketConnection()
    {
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();

        _state = ConnectionState.None;
    }

    public async Task ConnectAsync(Uri serverUri)
    {
        try
        {
            await _webSocket.ConnectAsync(serverUri, _cancellationTokenSource.Token);
            OnConnectionOpened?.Invoke();
            _state = ConnectionState.Open;
            
            Receive();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            _state = ConnectionState.Error;
        }
    }

    public async Task ConnectAsync(string server, uint port)
    {
        await ConnectAsync(new Uri($"ws://{server}:{port}"));
    }
    
    public async Task ConnectAsync(string server)
    {
        await ConnectAsync(new Uri($"ws://{server}"));
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", _cancellationTokenSource.Token);
            _state = ConnectionState.Closed;
            OnConnectionClosed?.Invoke();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            _state = ConnectionState.Error;
        }
    }

    public async Task SendAsync(byte[] data)
    {
        try
        {
            // byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            if(_webSocket.State == WebSocketState.Open)
                await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
                // await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
    }

    public async Task SendAsync(string data)
    {
        //Debug.Log("SEND: " + data);
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        await SendAsync(buffer);
    }

    private async void Receive()
    {
        byte[] buffer = new byte[_bufferSize];
        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await DisconnectAsync();
                }
                else
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessageReceived?.Invoke(message);
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            _state = ConnectionState.Error;
        }
    }

    public WebSocketState getState()
    {
        return _webSocket.State;
    }
}
