using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Security.Principal;
using Websocket.Tcp.Extensions.HttpParser.Extension;

namespace Websocket.Tcp.Server.Websocket.Context
{
    internal class TcpListenerWebsocketContext : WebSocketContext
    {
        private readonly WebSocket webSocket;
        private readonly CookieCollection cookieCollection;
        private readonly NameValueCollection headers;
        private readonly bool isAuthenticated;
        private readonly bool isLocal;
        private readonly bool isSecureConnection;
        private readonly string origin;
        private readonly Uri requestUri;
        private readonly string secWebSocketKey;
        private readonly IEnumerable<string> secWebSocketProtocols;
        private readonly string secWebSocketVersion;
        private readonly IPrincipal? user;

        internal TcpListenerWebsocketContext(
            WebSocket webSocket,
            CookieCollection cookieCollection,
            NameValueCollection headers,
            bool isAuthenticated,
            bool isLocal,
            bool isSecureConnection,
            string origin,
            Uri requestUri,
            string secWebSocketKey,
            IEnumerable<string> secWebSocketProtocols,
            string secWebSocketVersion,
            IPrincipal? user)
        {
            this.webSocket = webSocket;
            this.cookieCollection = cookieCollection;
            this.headers = headers;
            this.isAuthenticated = isAuthenticated;
            this.isLocal = isLocal;
            this.isSecureConnection = isSecureConnection;
            this.origin = origin;
            this.requestUri = requestUri;
            this.secWebSocketKey = secWebSocketKey;
            this.secWebSocketProtocols = secWebSocketProtocols;
            this.secWebSocketVersion = secWebSocketVersion;
            this.user = user;
        }

        public override CookieCollection CookieCollection => cookieCollection;
        public override NameValueCollection Headers => headers;
        public override bool IsAuthenticated => isAuthenticated;
        public override bool IsLocal => isLocal;
        public override bool IsSecureConnection => isSecureConnection;
        public override string Origin => origin;
        public override Uri RequestUri => requestUri;
        public override string SecWebSocketKey => secWebSocketKey;
        public override IEnumerable<string> SecWebSocketProtocols => secWebSocketProtocols;
        public override string SecWebSocketVersion => secWebSocketVersion;
        public override IPrincipal? User => user;
        public override WebSocket WebSocket => webSocket;

        internal static TcpListenerWebsocketContext Create(WebSocket webSocket, HttpRequestMessage requestMessage)
        {
            var headers = new NameValueCollection();

            foreach (var header in requestMessage.Headers)
                foreach (var headerValue in header.Value)
                    headers.Add(header.Key, headerValue);

            requestMessage.Headers.TryGetValue(WebsocketHttpKnownHeaders.Origin, out var origin);
            requestMessage.Headers.TryGetValue(WebsocketHttpKnownHeaders.SecWebSocketKey, out var secWebsocketKey);
            requestMessage.Headers.TryGetValue(WebsocketHttpKnownHeaders.SecWebSocketVersion, out var secWebSocketVersion);

            if (!requestMessage.Headers.TryGetValues(WebsocketHttpKnownHeaders.SecWebSocketProtocol, out var secWebsocketProtocols))
                secWebsocketProtocols = Enumerable.Empty<string>();

            bool isLocal = requestMessage.RequestUri!.Host == "localhost";

            return new TcpListenerWebsocketContext(webSocket, new CookieCollection(), headers, false, isLocal, false, origin!, requestMessage.RequestUri!, secWebsocketKey!, secWebsocketProtocols, secWebSocketVersion!, null);
        }
    }
}
