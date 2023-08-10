using System.Net.WebSockets;

namespace Websocket.Tcp.Proxy.Connections.Core.MessagePool.Models
{
    internal class SendMessage : WebSocketHeapMessage
    {
        public SendMessage(ArraySegment<byte> bytes, WebSocketMessageType messageType, bool endOfMessage)
            : base(bytes)
        {
            MessageType = messageType;
            EndOfMessage = endOfMessage;
        }

        public WebSocketMessageType MessageType { get; }
        public bool EndOfMessage { get; }
    }
}
