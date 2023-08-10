namespace Websocket.Tcp.Proxy.Connections.Interface
{
    public interface IWebSocketConnectionListener
    {
        void Start(CancellationToken cancellationToken);
        void Stop();
    }
}
