using Websocket.Tcp.Proxy.Connections.Core;
using Websocket.Tcp.Proxy.Connections.Core.Enums;

namespace Websocket.Tcp.Proxy.Options
{
    public class WebsocketTunnelOptions
    {
        private const int DefaultProxyConnection = 2;

        public required string ProjectName { get; set; }
        public required Uri ServerUri { get; set; }
        public ReconnectionPolicy? ReconnectionPolicy { get; set; }
        public int CountConnections { get; set; } = DefaultProxyConnection;
        public SendingMode SendingMode { get; set; }
    }
}
