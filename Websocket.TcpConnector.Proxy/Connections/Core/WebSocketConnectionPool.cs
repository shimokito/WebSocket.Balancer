using System.Collections;
using System.Collections.ObjectModel;
using System.Net.WebSockets;

namespace Websocket.Tcp.Proxy.Connections.Core
{
    internal class WebSocketConnectionPool : IReadOnlyList<InternalWebSocketConnection>, IDisposable
    {
        private readonly List<InternalWebSocketConnection> _connections;

        public int Count => _connections.Count;

        public WebSocketConnectionPool(Uri uri, ReconnectionPolicy? reconnectionPolicy, int countConnections)
        {
            _connections = new List<InternalWebSocketConnection>(countConnections);
            _connections.AddRange(CreateConnections(uri, reconnectionPolicy, countConnections));
        }

        public InternalWebSocketConnection this[int index] => _connections[index];

        public void AbortAll()
        {
            foreach (var connection in _connections)
            {
                connection.Abort();
            }
        }

        public Task CloseAllAsync(WebSocketCloseStatus closeStatus, string? closeStatusDescription, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>(Count);
            foreach (var connection in _connections)
            {
                tasks.Add(connection.CloseAsync(closeStatus, closeStatusDescription, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        public Task CloseAllOutputAsync(WebSocketCloseStatus closeStatus, string? closeStatusDescription, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>(Count);
            foreach (var connection in _connections)
            {
                tasks.Add(connection.CloseOutputAsync(closeStatus, closeStatusDescription, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        public bool IsAllState(WebSocketState state)
        {
            return _connections.All(x => x.State == state);
        }

        public bool IsAnyState(WebSocketState state)
        {
            return _connections.Any(x => x.State == state);
        }

        public IEnumerator<InternalWebSocketConnection> GetEnumerator()
        {
            return _connections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static IEnumerable<InternalWebSocketConnection> CreateConnections(Uri uri, ReconnectionPolicy? reconnectionPolicy, int countConnections)
        {
            for (var i = 0; i < countConnections; i++)
            {
                yield return new InternalWebSocketConnection(uri, reconnectionPolicy);
            }
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
            {
                connection.Dispose();
            }
        }
    }
}
