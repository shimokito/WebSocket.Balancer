using System.Threading.Channels;

namespace Websocket.Tcp.Proxy.Connections.Queue
{
    public abstract class WebSocketMessageQueue<TConnection>
        where TConnection : WebSocketConnection
    {
        private readonly Channel<WebSocketMessageQueueItem<TConnection>> _messageQueue;
        private CancellationTokenSource? _cts;

        public WebSocketMessageQueue()
        {
            _messageQueue = Channel.CreateUnbounded<WebSocketMessageQueueItem<TConnection>>();
        }

        public bool TryEnqueue(WebSocketMessageQueueItem<TConnection> message)
        {
            return _messageQueue.Writer.TryWrite(message);
        }

        public ValueTask EnqueueAsync(WebSocketMessageQueueItem<TConnection> message, CancellationToken cancellationToken = default)
        {
            return _messageQueue.Writer.WriteAsync(message, cancellationToken);
        }

        public void Start(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = ListenAsync(_cts.Token).ConfigureAwait(false);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (await _messageQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (!_messageQueue.Reader.TryRead(out var message))
                    continue;

                try
                {
                    await ProcessMessageAsync(message, cancellationToken);
                }
                catch
                {
                    //ignored
                }
            }
        }

        protected abstract Task ProcessMessageAsync(WebSocketMessageQueueItem<TConnection> item, CancellationToken cancellationToken);
    }
}
