namespace Websocket.Tcp.Extensions.HttpParser.Interface
{
    public interface IHttpBinaryRequestParser
    {
        void Parse(ArraySegment<byte> bytes);
    }
}