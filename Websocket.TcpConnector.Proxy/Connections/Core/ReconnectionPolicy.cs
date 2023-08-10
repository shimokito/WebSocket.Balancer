namespace Websocket.Tcp.Proxy.Connections.Core
{
    public class ReconnectionPolicy
    {
        public int ReconnectionCount { get; set; } = 10;
        public TimeSpan ReconnectionTimeout { get; set; } = Timeout.InfiniteTimeSpan;
        public bool ReconnectionEnabled { get; set; } = true;
    }
}
