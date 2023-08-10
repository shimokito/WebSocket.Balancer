using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Websocket.Tcp.Extensions.HttpParser.Extension;
using Websocket.Tcp.Server.Websocket.Context;

namespace Websocket.Tcp.Server.Utils
{
    internal static class WebsocketHandshakeHelper
    {
        const string CRLF = "\r\n";
        const string SecWebsocketVersion = "13";

        public static bool IsWebsocketRequest(HttpRequestMessage message)
        {
            if (message.Method != HttpMethod.Get)
                return false;

            return message.Headers.Connection.Contains("Upgrade")
                && message.Headers.Upgrade.Contains(new ProductHeaderValue("websocket"));
        }

        public static bool ValidateWebsocketRequest(HttpRequestMessage message)
        {
            return IsWebsocketRequest(message) && message.Headers.TryGetValue(WebsocketHttpKnownHeaders.SecWebSocketVersion, out var value) && value == SecWebsocketVersion;
        }

        public static byte[] BadRequest()
        {
            var httpSb = new StringBuilder();

            httpSb.Append("HTTP/1.1 400 Bad Request");
            httpSb.Append(CRLF);
            httpSb.Append(CRLF);

            return Encoding.UTF8.GetBytes(httpSb.ToString());
        }

        public static bool ContainsSubProtocol(HttpRequestMessage message, string? subProtocol)
        {
            if (subProtocol == null)
                return true;

            return message.Headers.TryGetValues(WebsocketHttpKnownHeaders.SecWebSocketProtocol, out var values)
                && values.Contains(subProtocol);
        }

        public static byte[] Handshake(HttpRequestMessage message, string? subProtocol)
        {
            if (!message.Headers.TryGetValues("Sec-WebSocket-Key", out var keys))
                throw new InvalidOperationException("Sec-WebSocket-Key header not found.");

            var swk = keys.FirstOrDefault();
            var swkaKey = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            var swkaSha1 = SHA1.HashData(Encoding.UTF8.GetBytes(swkaKey));
            var swkaBase64 = Convert.ToBase64String(swkaSha1);

            var str = new DefaultInterpolatedStringHandler(131, 2);
            str.AppendLiteral("HTTP/1.1 101 Switching Protocols");
            str.AppendLiteral(CRLF);
            str.AppendLiteral("Connection: Upgrade");
            str.AppendLiteral(CRLF);
            str.AppendLiteral("Upgrade: websocket");
            str.AppendLiteral(CRLF);
            str.AppendLiteral("Sec-WebSocket-Accept: ");
            str.AppendFormatted(swkaBase64);
            str.AppendLiteral(CRLF);
            if (!string.IsNullOrWhiteSpace(subProtocol))
            {
                str.AppendLiteral("Sec-WebSocket-Protocol: ");
                str.AppendFormatted(subProtocol);
                str.AppendLiteral(CRLF);
            }
            str.AppendLiteral(CRLF);

            return Encoding.UTF8.GetBytes(str.ToStringAndClear());
        }
    }
}