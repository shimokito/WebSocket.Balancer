using Websocket.Tcp.Proxy.Connections.Core.Enums;
using Websocket.Tcp.Proxy.Connections.Core.MessagePool;

namespace Websocket.Tcp.Proxy.Connections.Core
{
    internal class WebSocketMessageDistributor
    {
        private readonly WebSocketMessageHeap _messageHeap;
        private readonly SendingMode _mode;
        private readonly WebSocketConnectionPool _connections;
        private readonly ConnectionSwitcher? _switcher;

        public WebSocketMessageDistributor(WebSocketMessageHeap messageHeap, SendingMode mode, WebSocketConnectionPool connections)
        {
            _messageHeap = messageHeap;
            _mode = mode;
            _connections = connections;

            if (ConnectionSwitcher.SupportSwitchMode(_mode))
                _switcher = new ConnectionSwitcher(_connections, _mode);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (ConnectionSwitcher.SupportSwitchMode(_mode))
            {
                StartManagedConsumeMessage(cancellationToken);
            }
            else
            {
                StartAutoConsumeMessage(cancellationToken);
            }
        }

        private void StartAutoConsumeMessage(CancellationToken cancellationToken)
        {
            foreach (var connection in _connections)
            {
                _ = connection.StartRwProcessAsync(_messageHeap, cancellationToken).ConfigureAwait(false);
            }
        }

        private void StartManagedConsumeMessage(CancellationToken cancellationToken)
        {
            if (_switcher == null)
                throw new InvalidOperationException("Not supported managed mode.");

            _ = DistributeSendMessagesAsync(_messageHeap, _switcher, cancellationToken).ConfigureAwait(false);

            foreach (var connection in _connections)
            {
                _ = connection.ProcessReadAsync(_messageHeap, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task DistributeSendMessagesAsync(WebSocketMessageHeap messageHeap, ConnectionSwitcher switcher, CancellationToken cancellationToken)
        {
            while (true)
            {
                var sendMessage = await messageHeap.ReadSendMessageAsync(cancellationToken).ConfigureAwait(false);
                var connection = switcher.GetConnection();

                await connection.SendInstantAsync(sendMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private class ConnectionSwitcher
        {
            private readonly IReadOnlyList<InternalWebSocketConnection> _connections;
            private readonly SendingMode _mode;
            private readonly Random? _random;
            private readonly object _lock = new object();
            private int _index = 0;

            public ConnectionSwitcher(IReadOnlyList<InternalWebSocketConnection> connections, SendingMode mode)
            {
                _connections = connections;
                _mode = mode;

                if (mode == SendingMode.Chaotic)
                {
                    _random = new Random();
                }
            }

            public InternalWebSocketConnection GetConnection(bool increment = true)
            {
                var connection = GetConnection(_index);

                if (increment)
                    SetIndex(_mode);

                return connection;
            }

            public static bool SupportSwitchMode(SendingMode mode)
            {
                return mode == SendingMode.Chaotic || mode == SendingMode.Looped;
            }

            private int SetIndex(SendingMode mode)
            {
                switch (mode)
                {
                    case SendingMode.Chaotic:
                        return SetRandomIndex();
                    case SendingMode.Looped:
                        return SetLoopedIncrementedIndex();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode));
                }
            }

            private int SetRandomIndex()
            {
                if (_random == null)
                    throw new InvalidOperationException("Random seed not initalized.");

                lock (_lock)
                {
                    var rndIndex = _random.Next(0, _connections.Count);
                    _index = rndIndex;
                    return _index;
                }
            }

            private int SetLoopedIncrementedIndex()
            {
                lock (_lock)
                {
                    var incIndex = _index + 1;
                    var connectionsCount = _connections.Count;

                    if (incIndex == connectionsCount)
                    {
                        return _index = 0;
                    }
                    else
                    {
                        return _index = incIndex;
                    }
                }
            }

            private InternalWebSocketConnection GetConnection(int index)
            {
                return _connections[index];
            }
        }
    }
}
