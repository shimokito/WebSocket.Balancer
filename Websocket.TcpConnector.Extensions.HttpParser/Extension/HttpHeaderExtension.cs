using System.Net.Http.Headers;

namespace Websocket.Tcp.Extensions.HttpParser.Extension
{
    public static class HttpHeaderExtension
    {
        public static string? GetValue(this HttpHeaders headers, string name)
        {
            ArgumentNullException.ThrowIfNull(headers);
            return headers.GetValues(name)?.FirstOrDefault();
        }

        public static bool TryGetValue(this HttpHeaders headers, string name, out string? value)
        {
            ArgumentNullException.ThrowIfNull(headers);
            if (!headers.TryGetValues(name, out var values))
            {
                value = null;
                return false;
            }

            value = values?.FirstOrDefault();
            return true;
        }
    }
}
