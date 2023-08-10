using System.Buffers;
using System.Net.WebSockets;
using System.Timers;
using Websocket.Tcp.Proxy.Connections.Core.Enums;
using Websocket.Tcp.Proxy.Connections.Core.MessagePool;
using Websocket.Tcp.Proxy.Connections.Core.MessagePool.Models;

namespace Websocket.Tcp.Proxy.Connections.Core
{
    internal class InternalWebSocketConnection : IDisposable
    {
        public delegate Task AsyncWebSocketFactory(Uri uri, ClientWebSocket client, CancellationToken cancellationToken);

        private const int DefaultBufferSize = 4096;

        private readonly Uri _uri;
        private readonly ClientWebSocket _innerWebSocket;
        private readonly AsyncWebSocketFactory _factory;
        private readonly ReconnectionPolicy _reconnectionPolicy;
        private readonly System.Timers.Timer? _healthCheckTimer;
        private readonly object _syncLock = new object();
        private readonly int _receiveBufferSize;
        private bool _disposed = false;
        private bool _isReconnect = false;

        public WebSocketState State => _innerWebSocket.State;
        public WebSocketCloseStatus? CloseStatus => _innerWebSocket.CloseStatus;
        public string? CloseStatusDescription => _innerWebSocket.CloseStatusDescription;
        public string? SubProtocol => _innerWebSocket.SubProtocol;

        private volatile RwState _rwState;

        public InternalWebSocketConnection(Uri uri, ReconnectionPolicy? reconnectionPolicy)
            : this(uri, reconnectionPolicy, DefaultFactory, DefaultBufferSize)
        {
        }

        public InternalWebSocketConnection(Uri uri, ReconnectionPolicy? reconnectionPolicy, AsyncWebSocketFactory factory, int receiveBufferSize)
        {
            if (receiveBufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), "Receive buffer size too small.");

            _uri = uri;
            _innerWebSocket = new ClientWebSocket();
            _factory = factory;
            _reconnectionPolicy = reconnectionPolicy ?? new ReconnectionPolicy();
            _receiveBufferSize = receiveBufferSize;

            if (_reconnectionPolicy.ReconnectionEnabled)
            {
                _healthCheckTimer = CreateHealthCheckTimer(_innerWebSocket.Options.KeepAliveInterval);
            }
        }

        public Task StartRwProcessAsync(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var inputTask = ProcessReadAsyncCore(messageHeap, cancellationToken);
            var outputTask = ProcessWriteAsyncCore(messageHeap, cancellationToken);

            return Task.WhenAny(inputTask, outputTask)
                .ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception!;

                }, cancellationToken);
        }

        public Task ProcessWriteAsync(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return ProcessWriteAsyncCore(messageHeap, cancellationToken);
        }

        public Task ProcessReadAsync(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return ProcessReadAsyncCore(messageHeap, cancellationToken);
        }

        public Task<WebSocketReceiveResult> ReceiveInstantAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _innerWebSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public Task SendInstantAsync(SendMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return SendInstantAsync(message.MessageData, message.MessageType, message.EndOfMessage, cancellationToken);
        }

        public Task SendInstantAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public ValueTask SendInstantAsync(Memory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public void Abort()
        {
            _innerWebSocket.Abort();
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? closeStatusDescription, CancellationToken cancellationToken)
        {
            return _innerWebSocket.CloseAsync(closeStatus, closeStatusDescription, cancellationToken);
        }

        public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? closeStatusDescription, CancellationToken cancellationToken)
        {
            return _innerWebSocket.CloseOutputAsync(closeStatus, closeStatusDescription, cancellationToken);
        }

        private async Task ProcessWriteAsyncCore(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    await WriteMessageAsync(messageHeap, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (!IsConnectionOpened() && !IsConnectionClosed())
                    {
                        await ReconnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ProcessReadAsyncCore(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    await ReadMessageAsync(messageHeap, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (!IsConnectionOpened() && !IsConnectionClosed())
                    {
                        await ReconnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ReadMessageAsync(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            EnsureConnectionOpened();
            var buffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);
            try
            {
                var result = await ReceiveInstantAsync(buffer, cancellationToken);

                byte[] readBytes = new byte[result.Count];
                Array.Copy(buffer, 0, readBytes, 0, result.Count);

                messageHeap.Write(new ReceiveMessage(readBytes, this, result));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task WriteMessageAsync(WebSocketMessageHeap messageHeap, CancellationToken cancellationToken)
        {
            EnsureConnectionOpened();
            var message = await messageHeap.ReadSendMessageAsync(cancellationToken);
            await SendInstantAsync(message, cancellationToken);
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return ConnectAsyncCore(cancellationToken);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
                return;

            _disposed = true;
            _innerWebSocket.Dispose();
            _healthCheckTimer?.Dispose();

            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private System.Timers.Timer CreateHealthCheckTimer(TimeSpan interval, bool startTimer = true)
        {
            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += HealthCheck;

            if (startTimer)
                timer.Start();

            return timer;
        }

        private void HealthCheck(object? sender, ElapsedEventArgs args)
        {
            if (IsConnectionOpened() && !IsConnectionClosed())
                return;

            _ = ReconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<bool> ReconnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!StartReconnect())
                    return false;

                var reconnectionCount = _reconnectionPolicy.ReconnectionCount;
                var timeout = _reconnectionPolicy.ReconnectionTimeout;
                bool needWaitReconnection = timeout != Timeout.InfiniteTimeSpan;

                async Task<bool> ReconnectAsyncCore(bool withDelay)
                {
                    try
                    {
                        if (IsConnectionOpened())
                            return true;

                        if (withDelay)
                            await Task.Delay(timeout, cancellationToken);

                        await ConnectAsyncCore(cancellationToken);
                        return IsConnectionOpened();
                    }
                    catch (WebSocketException)
                    {
                        return false;
                    }
                    catch
                    {
                        throw;
                    }
                }

                if (await ReconnectAsyncCore(false))
                    return true;

                for (var i = 1; i < reconnectionCount; i++)
                {
                    if (await ReconnectAsyncCore(needWaitReconnection))
                        return true;
                }

                return false;
            }
            finally
            {
                EndReconnect();
            }
        }

        private bool StartReconnect()
        {
            if (!_reconnectionPolicy.ReconnectionEnabled)
                return false;

            lock (_syncLock)
            {
                if (_isReconnect)
                    return false;

                return _isReconnect = true;
            }
        }

        private void EndReconnect()
        {
            if (!_reconnectionPolicy.ReconnectionEnabled)
                return;

            lock (_syncLock)
            {
                if (!_isReconnect)
                    return;

                _isReconnect = false;
            }
        }

        private Task ConnectAsyncCore(CancellationToken cancellationToken)
        {
            if (IsConnectionOpened())
                return Task.CompletedTask;

            return _factory(_uri, _innerWebSocket, cancellationToken);
        }

        private bool IsConnectionOpened()
        {
            return State == WebSocketState.Open;
        }

        private bool IsConnectionClosed()
        {
            return State == WebSocketState.Closed ||
                   State == WebSocketState.CloseSent ||
                   State == WebSocketState.CloseReceived ||
                   State == WebSocketState.Aborted;
        }

        private void EnsureConnectionOpened()
        {
            if (!IsConnectionOpened())
                throw new WebSocketException("Connection not opened.");
        }

        private static async Task DefaultFactory(Uri uri, ClientWebSocket client, CancellationToken cancellationToken)
        {
            await client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }
    }
}
