using System.Net.WebSockets;
using Websocket.Tcp.Proxy.Connections.Core.Enums;
using Websocket.Tcp.Proxy.Connections.Core.MessagePool;
using Websocket.Tcp.Proxy.Connections.Core.MessagePool.Models;

namespace Websocket.Tcp.Proxy.Connections.Core
{
    public class MultiConnectedWebSocket : WebSocket
    {
        private readonly WebSocketMessageHeap _messageHeap;
        private readonly WebSocketConnectionPool _connectionPool;
        private readonly WebSocketMessageDistributor _distributor;

        public override WebSocketCloseStatus? CloseStatus
            => throw new NotSupportedException();
        public override string? CloseStatusDescription
            => throw new NotSupportedException();
        public override WebSocketState State
            => throw new NotSupportedException();
        public override string? SubProtocol
            => throw new NotSupportedException();

        public bool IsOpened => _connectionPool.IsAnyState(WebSocketState.Open);

        public MultiConnectedWebSocket(Uri uri, int countConnections, ReconnectionPolicy? reconnectionPolicy, SendingMode sendingMode = SendingMode.Looped)
        {
            _messageHeap = new WebSocketMessageHeap();
            _connectionPool = new WebSocketConnectionPool(uri, reconnectionPolicy, countConnections);
            _distributor = new WebSocketMessageDistributor(_messageHeap, sendingMode, _connectionPool);
        }

        public override void Abort()
        {
            _connectionPool.AbortAll();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return _connectionPool.CloseAllAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            throw new NotSupportedException($"Use overrided {nameof(CloseOutputAsync)}");
        }

        public Task CloseOutputAsync(WebSocketReceiveResult receiveResult, CancellationToken cancellationToken)
        {
            if (receiveResult is not ConnectionWebSocketReceiveResult connectionReceiveResult || connectionReceiveResult.MessageType != WebSocketMessageType.Close)
                return Task.FromException(new InvalidOperationException("Unexpected result close handshake."));

            return connectionReceiveResult.Connection.CloseOutputAsync(receiveResult.CloseStatus ?? WebSocketCloseStatus.Empty, receiveResult.CloseStatusDescription, cancellationToken);
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var reciveMessage = await _messageHeap.ReadReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
            var bytes = reciveMessage.MessageData;
            bytes.CopyTo(buffer);

            return new ConnectionWebSocketReceiveResult(reciveMessage.Connection, reciveMessage.ReceiveResult);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            var bytes = buffer.ToArray();
            return _messageHeap.WriteAsync(new SendMessage(bytes, messageType, endOfMessage), cancellationToken).AsTask();
        }

        public void Send(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
        {
            var bytes = buffer.ToArray();
            _messageHeap.Write(new SendMessage(bytes, messageType, endOfMessage));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            foreach (var connection in _connectionPool)
            {
                await connection.ConnectAsync(cancellationToken);
            }

            _distributor.Start(cancellationToken);
        }

        public override void Dispose()
        {
            _messageHeap.Dispose();
            _connectionPool.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
