using Websocket.Tcp.Proxy.Connections;

namespace Websocket.Tcp.Proxy.ConnectionResolver
{
    public interface IUnsubcriber<TConnection>
         where TConnection : WebSocketConnection
    {
        void Dispose();
    }
}
