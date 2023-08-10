using System.Net.WebSockets;

namespace Websocket.Tcp.Server.Websocket.Interface
{
    public interface IWebsocketConnectionConsumer
    {
        void Next(WebSocketContext context);
    }
}