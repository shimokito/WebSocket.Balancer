namespace Websocket.Tcp.Proxy.Connections.Core.Enums
{
    [Flags]
    public enum RwState
    {
        None = 0,
        Read = 1,
        Write = 2,
    }
}
