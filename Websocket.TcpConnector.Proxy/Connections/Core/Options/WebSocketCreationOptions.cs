namespace Websocket.Tcp.Proxy.Connections.Core.Options
{
    public class WebSocketCreationOptions
    {
        public required Uri Uri { get; set; }
        public ReconnectionPolicy? ReconnectionPolicy { get; set; }
        public string? SubProtocol { get; set; }
    }
}
