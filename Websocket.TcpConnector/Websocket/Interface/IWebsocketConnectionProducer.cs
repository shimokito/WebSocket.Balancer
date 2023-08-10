using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;

namespace Websocket.Tcp.Server.Websocket.Interface
{
    public interface IWebsocketConnectionProducer
    {
        ValueTask<bool> WaitConnectionAsync(CancellationToken cancellationToken = default);
        ValueTask<WebSocketContext> ReadAsync(CancellationToken cancellationToken = default);
        bool TryTake([MaybeNullWhen(false)] out WebSocketContext context);
    }
}