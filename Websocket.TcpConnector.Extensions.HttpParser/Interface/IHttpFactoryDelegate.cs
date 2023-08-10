namespace Websocket.Tcp.Extensions.HttpParser.Interface
{
    public interface IHttpFactoryDelegate : IDisposable
    {
        void OnMessageBegin(IHttpRequestFactory factory);

        void OnHeaderName(IHttpRequestFactory factory, string name);

        void OnHeaderValue(IHttpRequestFactory factory, string value);

        void OnHeadersEnd(IHttpRequestFactory factory);

        void OnTransferEncodingChunked(IHttpRequestFactory factory, bool isChunked);

        void OnChunkedLength(IHttpRequestFactory factory, int length);

        void OnChunkReceived(IHttpRequestFactory factory);

        void OnBody(IHttpRequestFactory factory, ArraySegment<byte> data);

        void OnMessageEnd(IHttpRequestFactory factory);

        void OnParserError(IHttpRequestFactory factory);
    }
}