using System.Net.WebSockets;

namespace Websocket.Tcp.Proxy.Connections.Core.MessagePool.Models
{
    internal class ReceiveMessage : WebSocketHeapMessage
    {
        public InternalWebSocketConnection Connection { get; }
        public WebSocketReceiveResult ReceiveResult { get; }

        public ReceiveMessage(ArraySegment<byte> bytes, InternalWebSocketConnection connection, WebSocketReceiveResult receiveResult)
            : base(bytes)
        {
            Connection = connection;
            ReceiveResult = receiveResult;
        }
    }
}
