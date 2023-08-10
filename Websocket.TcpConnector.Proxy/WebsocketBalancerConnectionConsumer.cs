using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Threading;
using System.Web;
using Websocket.Tcp.Proxy.ConnectionResolver;
using Websocket.Tcp.Proxy.Connections;
using Websocket.Tcp.Proxy.Options;
using Websocket.Tcp.Server.Websocket;
using Websocket.Tcp.Server.Websocket.Interface;

namespace Websocket.Tcp.Proxy
{
    public class WebsocketBalancerConnectionConsumer : WebsocketConnectionConsumer
    {
        private readonly WebsocketBalancerOptions _options;
        private readonly ServerConnectionResolver _serverConnections;
        private readonly ClientConnectionResolver _clientConnections;

        public WebsocketBalancerConnectionConsumer(IWebsocketConnectionProducer connectionProducer, IOptions<WebsocketBalancerOptions> proxyOptions)
            : base(connectionProducer)
        {
            _options = proxyOptions.Value ?? throw new ArgumentNullException(nameof(proxyOptions));
            _clientConnections = new ClientConnectionResolver();
            _serverConnections = new ServerConnectionResolver(_options.Tunnels ?? new List<WebsocketTunnelOptions>
            {
                #if DEBUG
                new WebsocketTunnelOptions
                {
                    ProjectName = "Echo",
                    ServerUri = new Uri("wss://ws.postman-echo.com/raw")
                }
                #endif
            }, _clientConnections);
        }

        public override void Next(WebSocketContext context)
        {
            var clientId = GetSynchronizationToken(context);
            var proxyClient = new WebSocketClientConnection(clientId, context.WebSocket, _serverConnections);
            var unsubsriber = _clientConnections.Add(clientId, proxyClient);
            proxyClient.Register(unsubsriber);
            proxyClient.Start();
        }

        protected override void StartListen(CancellationToken cancellationToken)
        {
            base.StartListen(cancellationToken);
            InitServers(cancellationToken);
        }

        private void InitServers(CancellationToken cancellationToken)
        {
            foreach (var connection in _serverConnections)
            {
                connection.Start(cancellationToken);
            }
        }

        private static Guid GetSynchronizationToken(WebSocketContext context)
        {
            var queryStr = context.RequestUri.Query;

            if (string.IsNullOrWhiteSpace(queryStr))
                return Guid.NewGuid();

            var query = HttpUtility.ParseQueryString(queryStr);
            var token = query.Get("token");

            if (token == null)
                return Guid.NewGuid();

            if (!Guid.TryParse(token, out var result))
                result = Guid.NewGuid();

            return result;
        }
    }
}
