namespace Websocket.Tcp.Proxy.Connections.Core.MessagePool.Models
{
    public abstract class WebSocketHeapMessage
    {
        public ArraySegment<byte> MessageData { get; set; }

        public WebSocketHeapMessage(ArraySegment<byte> bytes)
        {
            MessageData = bytes;
        }
    }
}
