using HttpMachine;
using IHttpMachine;
using Websocket.Tcp.Extensions.HttpParser.Interface;

namespace Websocket.Tcp.Extensions.HttpParser.Factory
{
    internal class CombinedFactoryDelegate : HttpParserDelegate, IHttpRequestFactory
    {
        private readonly IHttpRequestFactoryDelegate _factoryDelegate;
        private readonly IHttpRequestFactory _factory;

        public CombinedFactoryDelegate(IHttpRequestFactoryDelegate factoryDelegate, IHttpRequestFactory factory)
        {
            _factoryDelegate = factoryDelegate;
            _factory = factory;
        }

        public override void OnMessageBegin(IHttpCombinedParser combinedParser)
        {
            _factoryDelegate.OnMessageBegin(_factory);
        }

        public override void OnHeaderName(IHttpCombinedParser combinedParser, string headerName)
        {
            _factoryDelegate.OnHeaderName(_factory, headerName);
        }

        public override void OnHeaderValue(IHttpCombinedParser combinedParser, string value)
        {
            _factoryDelegate.OnHeaderValue(_factory, value);
        }

        public override void OnHeadersEnd(IHttpCombinedParser combinedParser)
        {
            _factoryDelegate.OnHeadersEnd(_factory);
        }

        public override void OnTransferEncodingChunked(IHttpCombinedParser combinedParser, bool isChunked)
        {
            _factoryDelegate.OnTransferEncodingChunked(_factory, isChunked);
        }

        public override void OnChunkedLength(IHttpCombinedParser combinedParser, int length)
        {
            _factoryDelegate.OnChunkedLength(_factory, length);
        }

        public override void OnChunkReceived(IHttpCombinedParser combinedParser)
        {
            _factoryDelegate.OnChunkReceived(_factory);
        }

        public override void OnBody(IHttpCombinedParser combinedParser, ArraySegment<byte> data)
        {
            _factoryDelegate.OnBody(_factory, data);
        }

        public override void OnParserError()
        {
            _factoryDelegate.OnParserError(_factory);
        }

        public override void OnMethod(IHttpCombinedParser combinedParser, string method)
        {
            _factoryDelegate.OnMethod(_factory, method);
        }

        public override void OnRequestUri(IHttpCombinedParser combinedParser, string requestUri)
        {
            _factoryDelegate.OnRequestUri(_factory, requestUri);
        }

        public override void OnPath(IHttpCombinedParser combinedParser, string path)
        {
            _factoryDelegate.OnPath(_factory, path);
        }

        public override void OnFragment(IHttpCombinedParser combinedParser, string fragment)
        {
            _factoryDelegate.OnQueryString(_factory, fragment);
        }

        public override void OnQueryString(IHttpCombinedParser combinedParser, string queryString)
        {
            _factoryDelegate.OnQueryString(_factory, queryString);
        }

        public override void OnRequestType(IHttpCombinedParser combinedParser)
        {
        }

        public override void OnResponseType(IHttpCombinedParser combinedParser)
        {
        }

        public override void OnResponseCode(IHttpCombinedParser combinedParser, int statusCode, string statusReason)
        {
        }

        public override void OnMessageEnd(IHttpCombinedParser combinedParser)
        {
            _factoryDelegate.OnMessageEnd(_factory);
        }

        public override void Dispose()
        {
            _factoryDelegate.Dispose();
            base.Dispose();
        }

        public HttpRequestMessage? BuildHttpRequest()
        {
            return _factory.BuildHttpRequest();
        }
    }
}
