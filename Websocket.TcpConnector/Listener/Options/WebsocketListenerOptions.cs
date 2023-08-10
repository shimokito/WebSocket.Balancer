namespace Websocket.Tcp.Server.Listener.Options
{
    public class WebsocketListenerOptions : ListenerOptions
    {
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);
        public string? SubProtocol { get; set; }
    }
}
