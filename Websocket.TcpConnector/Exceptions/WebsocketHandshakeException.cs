namespace Websocket.Tcp.Server.Exceptions
{
    public class WebsocketHandshakeException : Exception
    {
        public WebsocketHandshakeException()
            : base("Websocket handshake exception")
        {

        }

        public WebsocketHandshakeException(Exception? innerException)
            : base("Websocket handshake exception", innerException)
        {

        }
    }
}
