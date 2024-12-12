namespace DefaultNamespace
{
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
            this._server = server;
            return this;
        }

        public WebRTCClientBuilder SetPort(uint port)
        {
            this._port = port;
            return this;
        }

        public WebRTCClientBuilder SetSessionId(string sessionId)
        {
            this._sessionId = sessionId;
            return this;
        }

        public WebRTCClientBuilder SetPeerId(string peerId)
        {
            this._peerId = peerId;
            return this;
        }
        public WebRTCClientBuilder SetChannel(string channelId)
        {
            this._channelId = channelId;
            return this;
        }

        public WebRTCClient Build()
        {
            return _client;
        }
    }
}