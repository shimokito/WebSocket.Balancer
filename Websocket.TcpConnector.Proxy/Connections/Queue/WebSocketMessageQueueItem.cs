using System.Net.WebSockets;

namespace Websocket.Tcp.Proxy.Connections.Queue
{
    public class WebSocketMessageQueueItem<TConnection>
        where TConnection : WebSocketConnection
    {
        public TConnection Connection { get; }
        public ArraySegment<byte> Data { get; }
        public WebSocketMessageType MessageType { get; }
        public bool EndOfMessage { get; }

        public WebSocketMessageQueueItem(TConnection connection, ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage)
        {
            Connection = connection;
            Data = data;
            MessageType = messageType;
            EndOfMessage = endOfMessage;
        }
    }
}
