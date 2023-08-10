using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Websocket.Tcp.Proxy.Connections;

namespace Websocket.Tcp.Proxy.ConnectionResolver
{
    public class ClientConnectionResolver
    {
        private readonly ConcurrentDictionary<Guid, ScopeWebSocketClientConnections> _batchConnections;

        public ClientConnectionResolver()
        {
            _batchConnections = new ConcurrentDictionary<Guid, ScopeWebSocketClientConnections>();
        }

        public ScopeWebSocketClientConnections Resolve(Guid token)
        {
            if (!TryResolve(token, out var connection))
                throw new InvalidOperationException("Unable to resolve connection.");

            return connection;
        }

        public bool TryResolve(Guid token, [MaybeNullWhen(false)] out ScopeWebSocketClientConnections connection)
        {
            return _batchConnections.TryGetValue(token, out connection);
        }

        public IUnsubcriber<WebSocketClientConnection> Add(Guid token, WebSocketClientConnection connection)
        {
            var scope = _batchConnections.GetOrAdd(token, new ScopeWebSocketClientConnections());
            scope.Add(connection);
            return new ClientUnsubsriber(token, this);
        }

        public bool Remove(WebSocketClientConnection connection)
        {
            if (!_batchConnections.TryGetValue(connection.Id, out var scope))
                return false;

            var isSuccess = scope.Remove(connection);

            if (scope.Count == 0)
                _batchConnections.Remove(connection.Id, out _);

            return isSuccess;
        }

        public bool Remove(Guid token)
        {
            if (!_batchConnections.TryGetValue(token, out var scope))
                return false;

            var isSuccess = scope.Remove(token);

            if (scope.Count == 0)
                _batchConnections.Remove(token, out _);

            return isSuccess;
        }

        private class ClientUnsubsriber : IUnsubcriber<WebSocketClientConnection>
        {
            private readonly Guid _token;
            private readonly ClientConnectionResolver _resolver;

            public ClientUnsubsriber(Guid token, ClientConnectionResolver resolver)
            {
                _token = token;
                _resolver = resolver;
            }

            public void Dispose()
            {
                _resolver.Remove(_token);
            }
        }
    }
}
