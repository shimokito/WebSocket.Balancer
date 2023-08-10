using System.Net.WebSockets;

namespace Websocket.Tcp.Proxy.Connections.Core
{
    internal class ConnectionWebSocketReceiveResult : WebSocketReceiveResult
    {
        public InternalWebSocketConnection Connection { get; }

        public ConnectionWebSocketReceiveResult(InternalWebSocketConnection connection, WebSocketReceiveResult receiveResult)
            : base(receiveResult.Count, receiveResult.MessageType, receiveResult.EndOfMessage, receiveResult.CloseStatus, receiveResult.CloseStatusDescription)
        {
            Connection = connection;
        }
    }
}
