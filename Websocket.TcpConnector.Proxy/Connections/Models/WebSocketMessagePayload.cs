using System.Net.WebSockets;

namespace Websocket.Tcp.Proxy.Connections.Models
{
    public record WebSocketMessagePayload
    {
        public WebSocketMessageType MessageType { get; }
        public bool EndOfMessage => Flags.HasFlag(WebSocketMessageFlags.EndOfMessage);
        public WebSocketMessageFlags Flags { get; }

        public WebSocketMessagePayload(WebSocketMessageType messageType, bool endOfMessage)
        {
            MessageType = messageType;
            if (endOfMessage)
                Flags |= WebSocketMessageFlags.EndOfMessage;
        }
        public WebSocketMessagePayload(WebSocketMessageType messageType, WebSocketMessageFlags flags)
        {
            MessageType = messageType;
            Flags = flags;
        }
    }
}
