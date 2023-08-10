using Websocket.Tcp.Extensions.HttpParser.Interface;

namespace Websocket.Tcp.Server.Utils.HttpFactory
{
    internal class WebsocketHandshakeHttpRequestFactoryDelegate : IHttpRequestFactoryDelegate, IHttpRequestFactory
    {
        private bool _isStartHttpHandle = false;
        private bool _isEndHttpHandle = false;
        private bool _isHeaderHandled = false;
        private string? _headerName;

        private UriBuilder? _uriBuilder;

        private HttpRequestMessage? _requestMessage;

        public HttpRequestMessage? RequestMessage => _requestMessage;
        public bool IsHttpHandle => _isStartHttpHandle && _isEndHttpHandle;

        public WebsocketHandshakeHttpRequestFactoryDelegate()
        {
        }

        public HttpRequestMessage? BuildHttpRequest()
        {
            if (!IsHttpHandle)
                return null;

            return _requestMessage;
        }

        public void OnFragment(IHttpRequestFactory factory, string fragment)
        {
            ThrowIfRequestMessageNull();
            _uriBuilder!.Fragment = fragment;
        }

        public void OnHeaderName(IHttpRequestFactory factory, string name)
        {
            if (_isHeaderHandled)
                return;

            _headerName = name;
        }

        public void OnHeadersEnd(IHttpRequestFactory factory)
        {
            _isHeaderHandled = true;
        }

        public void OnHeaderValue(IHttpRequestFactory factory, string value)
        {
            if (_isHeaderHandled)
                return;

            ThrowIfRequestMessageNull();
            _requestMessage!.Headers.Add(_headerName!, value);
            _headerName = null;
        }

        public void OnMessageBegin(IHttpRequestFactory factory)
        {
            _isStartHttpHandle = true;
            _uriBuilder = new UriBuilder();
            _requestMessage = new HttpRequestMessage();
        }

        public void OnMessageEnd(IHttpRequestFactory factory)
        {
            ThrowIfRequestMessageNull();

            _isEndHttpHandle = true;
            _requestMessage!.RequestUri = _uriBuilder!.Uri;
        }

        public void OnMethod(IHttpRequestFactory factory, string method)
        {
            ThrowIfRequestMessageNull();
            _requestMessage!.Method = new HttpMethod(method);
        }

        public void OnParserError(IHttpRequestFactory factory)
        {
            throw new HttpRequestException("Websocket handshake parse error.");
        }

        public void OnPath(IHttpRequestFactory factory, string path)
        {
            ThrowIfRequestMessageNull();
            _uriBuilder!.Path = path;
        }

        public void OnQueryString(IHttpRequestFactory factory, string queryString)
        {
            ThrowIfRequestMessageNull();
            _uriBuilder!.Query = queryString;
        }

        public void OnRequestUri(IHttpRequestFactory factory, string requestUri)
        {
            ThrowIfRequestMessageNull();

            if (Uri.TryCreate(requestUri, UriKind.Absolute, out var uri))
                _uriBuilder!.Uri.MakeRelativeUri(uri);
        }

        #region Not supported

        public void OnBody(IHttpRequestFactory factory, ArraySegment<byte> data)
        {
            ThrowNotSupportedException();
        }

        public void OnChunkedLength(IHttpRequestFactory factory, int length)
        {
            ThrowNotSupportedException();
        }

        public void OnChunkReceived(IHttpRequestFactory factory)
        {
            ThrowNotSupportedException();
        }

        public void OnTransferEncodingChunked(IHttpRequestFactory factory, bool isChunked)
        {
            ThrowNotSupportedException();
        }

        #endregion Not supported

        public void Reset(bool disposeRequest)
        {
            _isStartHttpHandle = false;
            _isEndHttpHandle = false;
            _isHeaderHandled = false;

            if (disposeRequest)
            {
                _requestMessage?.Dispose();
            }
        }

        public void Dispose()
        {
            Reset(true);
            GC.SuppressFinalize(this);
        }

        private void ThrowNotSupportedException()
        {
            throw new NotSupportedException("Unexpected websocket http request handshake.");
        }

        private void ThrowIfRequestMessageNull()
        {
            ArgumentNullException.ThrowIfNull(_requestMessage, "Request message");
        }
    }
}
