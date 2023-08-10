using System.Net.WebSockets;
using Websocket.Tcp.Server.Websocket.Interface;

namespace Websocket.Tcp.Server.Websocket
{
    public abstract class WebsocketConnectionConsumer : IWebsocketConnectionConsumer
    {
        private const int StartedState = 1;
        private const int StoppedState = 0;

        private int _state = StoppedState;
        private readonly IWebsocketConnectionProducer _connectionProducer;
        private CancellationTokenSource? _cts;

        public WebsocketConnectionConsumer(IWebsocketConnectionProducer connectionProducer)
        {
            _connectionProducer = connectionProducer;
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _state, StartedState, StoppedState) == StartedState)
                throw new InvalidOperationException("Connection provider alredy started.");

            var linkedToken = LinkToLocal(cancellationToken);
            StartListen(linkedToken);
        }

        protected virtual void StartListen(CancellationToken cancellationToken)
        {
            _ = ListenAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _state, StoppedState, StartedState) == StoppedState)
                return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
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

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (await _connectionProducer.WaitConnectionAsync(cancellationToken))
            {
                if (!_connectionProducer.TryTake(out var context))
                    continue;

                Next(context);
            }
        }

        public abstract void Next(WebSocketContext context);
    }
}
