using System.Net.WebSockets;
using Websocket.Tcp.Proxy.Connections.Interface;
using Websocket.Tcp.Proxy.Connections.Models;

namespace Websocket.Tcp.Proxy.Connections
{
    public abstract class WebSocketConnection : IWebSocketConnectionSender, IWebSocketConnectionListener, IDisposable
    {
        private readonly object _internalLock = new object();
        private readonly WebSocket _webSocket;
        private bool _isListen = false;
        private CancellationTokenSource? _cts;

        public WebSocketConnection(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public virtual Task SendAsync(ArraySegment<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken = default)
        {
            return _webSocket.SendAsync(buffer, payload.MessageType, payload.EndOfMessage, cancellationToken);
        }

        public virtual ValueTask SendAsync(Memory<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken = default)
        {
            return _webSocket.SendAsync(buffer, payload.MessageType, payload.EndOfMessage, cancellationToken);
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            lock (_internalLock)
            {
                if (_isListen)
                    return;

                _isListen = true;
                var linkedToken = LinkToLocal(cancellationToken);
                StartListening(cancellationToken);
            }
        }

        public void Stop()
        {
            lock (_internalLock)
            {
                if (!_isListen)
                    return;

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                _isListen = false;
            }
        }

        public Task ListenAsync(CancellationToken cancellationToken)
        {
            return ListenAsync(_webSocket, cancellationToken);
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return _webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        protected virtual void StartListening(CancellationToken cancellationToken)
        {
            _ = ListenAsync(_webSocket, cancellationToken).ConfigureAwait(false);
        }

        private CancellationToken LinkToLocal(CancellationToken cancellationToken)
        {
            CancellationTokenSource? cts = _cts;
            if (cts == null)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }
            else if (cancellationToken != default)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            }

            _cts = cts;
            return _cts.Token;
        }

        protected abstract Task ListenAsync(WebSocket webSocket, CancellationToken cancellationToken);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webSocket.Dispose();
            }
        }
    }
}
