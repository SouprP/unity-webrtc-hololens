// using System;
// using System.Threading.Tasks;
// // using Hololens.Assets.Scripts.Connection.Model;
// // using Hololens.Assets.Scripts.Connection;
// using UnityEngine;
// #if ENABLE_WINMD_SUPPORT
// using Windows.Networking.Sockets;
// using Windows.Storage.Streams;
// #endif
//
// namespace Hololens.Assets.Scripts.Connection.Manager
// {
//     public class WebSocketManager
//     {
//         private readonly string _endpoint;
//
// #if ENABLE_WINMD_SUPPORT
//         private StreamWebSocket _webSocket;
//         private DataWriter _dataWriter;
//         private DataReader _dataReader;
// #endif
//
//         public WebSocketManager(string endpoint)
//         {
//             _endpoint = endpoint;
//         }
//
//         // Platform-independent interface for Connect
//         public async Task ConnectAsync()
//         {
// #if ENABLE_WINMD_SUPPORT
//             try
//             {
//                 _webSocket = new StreamWebSocket();
//                 await _webSocket.ConnectAsync(new Uri(_endpoint));
//                 _dataWriter = new DataWriter(_webSocket.OutputStream);
//                 _dataReader = new DataReader(_webSocket.InputStream);
//                 Debug.Log($"Connected to {_endpoint}");
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Failed to connect to {_endpoint}: {ex.Message}");
//                 throw;
//             }
// #else
//             Debug.LogWarning("ConnectAsync is not supported outside UWP.");
//             await Task.CompletedTask; // Mock implementation
// #endif
//         }
//
//         // Platform-independent interface for Send
//         public async Task SendAsync(Packet packet)
//         {
// #if ENABLE_WINMD_SUPPORT
//             try
//             {
//                 byte[] serializedData = MessagePackSerializer.Serialize(packet);
//                 _dataWriter.WriteBytes(serializedData);
//                 await _dataWriter.StoreAsync();
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error sending raw message: {ex.Message}");
//                 throw;
//             }
// #else
//             Debug.LogWarning("SendAsync is not supported outside UWP.");
//             await Task.CompletedTask; // Mock implementation
// #endif
//         }
//
//         // Platform-independent interface for Receive
//         public async Task ReceiveAsync(Action<Packet> onPacketReceived)
//         {
// #if ENABLE_WINMD_SUPPORT
//             byte[] buffer = new byte[1024];
//             try
//             {
//                 uint size = await _dataReader.LoadAsync((uint)buffer.Length);
//                 if (size == 0)
//                 {
//                     Debug.LogWarning("WebSocket closed by server.");
//                     return;
//                 }
//
//                 _dataReader.ReadBytes(buffer);
//                 Packet packet = MessagePackSerializer.Deserialize<Packet>(buffer);
//                 onPacketReceived?.Invoke(packet);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error receiving message: {ex.Message}");
//                 throw;
//             }
// #else
//             Debug.LogWarning("ReceiveAsync is not supported outside UWP.");
//             await Task.CompletedTask; // Mock implementation
// #endif
//         }
//
//         // Platform-independent interface for Close
//         public async Task CloseAsync()
//         {
// #if ENABLE_WINMD_SUPPORT
//             if (_webSocket != null)
//             {
//                 try
//                 {
//                     _dataWriter?.DetachStream();
//                     _dataWriter?.Dispose();
//                     _dataReader?.Dispose();
//                     _webSocket.Dispose();
//                     _webSocket = null;
//                     Debug.Log($"Connection to {_endpoint} closed.");
//                 }
//                 catch (Exception ex)
//                 {
//                     Debug.LogError($"Error closing connection: {ex.Message}");
//                     throw;
//                 }
//             }
// #else
//             Debug.LogWarning("CloseAsync is not supported outside UWP.");
//             await Task.CompletedTask; // Mock implementation
// #endif
//         }
//
//         // Platform-independent method to check WebSocket state
//         public bool IsWebSocketOpen()
//         {
// #if ENABLE_WINMD_SUPPORT
//             return _webSocket != null;
// #else
//             Debug.LogWarning("IsWebSocketOpen is not supported outside UWP.");
//             return false; // Mock implementation
// #endif
//         }
//     }
// }