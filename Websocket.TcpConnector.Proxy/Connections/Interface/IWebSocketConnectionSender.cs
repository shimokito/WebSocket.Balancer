using Websocket.Tcp.Proxy.Connections.Models;

namespace Websocket.Tcp.Proxy.Connections.Interface
{
    public interface IWebSocketConnectionSender
    {
        Task SendAsync(ArraySegment<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken = default);
        ValueTask SendAsync(Memory<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken = default);
    }
}
