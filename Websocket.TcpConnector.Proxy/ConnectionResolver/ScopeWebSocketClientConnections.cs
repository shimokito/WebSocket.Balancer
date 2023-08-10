using System.Collections.ObjectModel;
using Websocket.Tcp.Proxy.Connections;
using Websocket.Tcp.Proxy.Connections.Interface;
using Websocket.Tcp.Proxy.Connections.Models;

namespace Websocket.Tcp.Proxy.ConnectionResolver
{
    public class ScopeWebSocketClientConnections : IWebSocketConnectionSender
    {
        private readonly ClientKeyedCollection _clients;

        public int Count => _clients.Count;

        public ScopeWebSocketClientConnections()
        {
            _clients = new ClientKeyedCollection();
        }

        public void Add(WebSocketClientConnection connection)
        {
            _clients.Add(connection);
        }

        public bool Remove(WebSocketClientConnection connection)
        {
            return _clients.Remove(connection);
        }

        public bool Remove(Guid token)
        {
            return _clients.Remove(token);
        }

        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>(_clients.Count);
            foreach (var client in _clients)
            {
                tasks.Add(client.SendAsync(buffer, payload, cancellationToken));
            }
            return Task.WhenAll(tasks);
        }

        public ValueTask SendAsync(Memory<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            foreach (var client in _clients)
            {
                var sendTask = client.SendAsync(buffer, payload, cancellationToken);

                if (!sendTask.IsCompleted)
                    tasks.Add(sendTask.AsTask());
            }

            if (tasks.Count == 0)
                return ValueTask.CompletedTask;

            return new ValueTask(Task.WhenAll(tasks));
        }

        private class ClientKeyedCollection : KeyedCollection<Guid, WebSocketClientConnection>
        {
            protected override Guid GetKeyForItem(WebSocketClientConnection item)
            {
                return item.Id;
            }
        }
    }
}
