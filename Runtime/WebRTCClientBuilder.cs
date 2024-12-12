
public class WebRTCClientBuilder
{
    private WebRTCClient _client;
    
    private string _server = "localhost"; 
    private uint _port = 8765;
    private string _sessionId = "S1";
    private string _peerId = "unity_client_p1"; 
    private string _channelId = "chat";

    public WebRTCClientBuilder()
    {
        _client = new WebRTCClient();
    }

    public WebRTCClientBuilder SetServer(string server)
    {
        _client.SetServer(server);
        return this;
    }

    public WebRTCClientBuilder SetPort(uint port)
    {
        _client.SetPort(port);
        return this;
    }

    public WebRTCClientBuilder SetSessionId(string sessionId)
    {
        _client.SetSessionId(sessionId);
        return this;
    }

    public WebRTCClientBuilder SetPeerId(string peerId)
    {
        _client.SetPeerId(peerId);
        return this;
    }
    public WebRTCClientBuilder SetChannel(string channelId)
    {
        _client.SetChannel(channelId);
        return this;
    }

    public WebRTCClientBuilder EnableDebug(bool enable)
    {
        _client.EnableDebug(enable);
        return this;
    }

    public WebRTCClient Build()
    {
        return _client;
    }
}
