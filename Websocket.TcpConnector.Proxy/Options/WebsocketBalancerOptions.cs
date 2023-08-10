using Microsoft.Extensions.Options;

namespace Websocket.Tcp.Proxy.Options
{
    public class WebsocketBalancerOptions : IOptions<WebsocketBalancerOptions>
    {
        WebsocketBalancerOptions IOptions<WebsocketBalancerOptions>.Value
            => this;

        public List<WebsocketTunnelOptions>? Tunnels { get; set; }
    }
}
