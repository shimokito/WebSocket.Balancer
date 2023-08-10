using HttpMachine;
using IHttpMachine;
using IHttpMachine.Model;
using Websocket.Tcp.Extensions.HttpParser.Interface;

namespace Websocket.Tcp.Extensions.HttpParser.Factory
{
    internal class DefaultHttpFactory : HttpParserDelegate, IHttpRequestFactory
    {
        private bool _isStartHttpHandle = false;
        private bool _isEndHttpHandle = false;

        public bool IsHttpHandled => _isStartHttpHandle && _isEndHttpHandle;

        public virtual HttpRequestMessage BuildHttpRequest()
        {
            UriBuilder uriBuilder;
            if (Uri.TryCreate(HttpRequestResponse.RequestUri, UriKind.Absolute, out var uri))
                uriBuilder = new UriBuilder(uri);
            else
                uriBuilder = new UriBuilder();

            if (!HttpRequestResponse.Headers.TryGetValue("HOST", out var hosts))
                throw new InvalidOperationException("Host not found.");

            var hostUrl = hosts?.FirstOrDefault();
            if (hostUrl != null)
            {
                var c = hostUrl.IndexOf(':');
                if (c == -1 || c == 0)
                {
                    throw new InvalidOperationException("");
                }

                var host = hostUrl.AsSpan(0, c).Trim().ToString();
                var port = hostUrl.AsSpan(c, hostUrl.Length - c).Trim().ToString();

                if (!int.TryParse(port, out var portNumber))
                    portNumber = -1;

                uriBuilder.Host = host;
                uriBuilder.Port = portNumber;
            }

            uriBuilder.Query = HttpRequestResponse.QueryString;
            uriBuilder.Fragment = HttpRequestResponse.Fragment;

            var httpRequest = new HttpRequestMessage(new HttpMethod(HttpRequestResponse.Method), uriBuilder.Uri)
            {
                Version = new Version(HttpRequestResponse.MajorVersion, HttpRequestResponse.MinorVersion),
            };

            foreach (var header in HttpRequestResponse.Headers)
                httpRequest.Headers.Add(header.Key, header.Value);

            return httpRequest;
        }

        public override void OnMessageBegin(IHttpCombinedParser combinedParser)
        {
            _isStartHttpHandle = true;
            base.OnMessageBegin(combinedParser);
        }

        public override void OnMessageEnd(IHttpCombinedParser combinedParser)
        {
            _isEndHttpHandle = true;
            base.OnMessageEnd(combinedParser);
        }

        public void Reset()
        {
            _isEndHttpHandle = false;
            _isStartHttpHandle = false;
        }

        public override void Dispose()
        {
            Reset();
            base.Dispose();
        }
    }
}