namespace Websocket.Tcp.Server.Listener.Options
{
    public class ListenerOptions
    {
        private static readonly int DefaultCountListener = 4;

        public int CountListeners { get; set; } = DefaultCountListener;
    }
}
