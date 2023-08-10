using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Websocket.Tcp.Proxy.Connections;
using Websocket.Tcp.Proxy.Options;

namespace Websocket.Tcp.Proxy.ConnectionResolver
{
    public class ServerConnectionResolver : IEnumerable<WebSocketServerConnection>
    {
        private readonly ConcurrentDictionary<string, WebSocketServerConnection> _connections;
        private readonly ClientConnectionResolver _connectionResolver;

        private ServerConnectionResolver(IEnumerable<WebsocketTunnelOptions> tunnels, ClientConnectionResolver connectionResolver, IEqualityComparer<string> equalityComparer)
        {
            if (tunnels == null)
                throw new ArgumentNullException(nameof(tunnels));

            _connectionResolver = connectionResolver;
            _connections = new ConcurrentDictionary<string, WebSocketServerConnection>(CreateProxies(tunnels), equalityComparer);
        }
        public ServerConnectionResolver(IEnumerable<WebsocketTunnelOptions> tunnels, ClientConnectionResolver connectionResolver)
            : this(tunnels, connectionResolver, StringComparer.OrdinalIgnoreCase)
        {
        }

        private IEnumerable<KeyValuePair<string, WebSocketServerConnection>> CreateProxies(IEnumerable<WebsocketTunnelOptions> tunnels)
        {
            foreach (var tunnel in tunnels)
                yield return new KeyValuePair<string, WebSocketServerConnection>(
                    GetToken(tunnel),
                    new WebSocketServerConnection(tunnel, _connectionResolver));
        }

        protected virtual string GetToken(WebsocketTunnelOptions tunnelOptions)
        {
            return tunnelOptions.ProjectName;
        }

        public WebSocketServerConnection Resolve(string token)
        {
            if (!TryResolve(token, out var connection))
                throw new InvalidOperationException("Unable to resolve connection.");

            return connection;
        }

        public bool TryResolve(string token, [MaybeNullWhen(false)] out WebSocketServerConnection connection)
        {
            return _connections.TryGetValue(token, out connection);
        }

        public IEnumerator<WebSocketServerConnection> GetEnumerator()
        {
            return _connections.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
