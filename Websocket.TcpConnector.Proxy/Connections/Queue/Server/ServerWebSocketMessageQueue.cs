using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Websocket.Tcp.Proxy.ConnectionResolver;
using Websocket.Tcp.Proxy.Connections.Models;

namespace Websocket.Tcp.Proxy.Connections.Queue.Server
{
    public class ServerWebSocketMessageQueue : WebSocketMessageQueue<WebSocketServerConnection>
    {
        private readonly ClientConnectionResolver _connectionResolver;

        public ServerWebSocketMessageQueue(ClientConnectionResolver connectionResolver)
        {
            _connectionResolver = connectionResolver;
        }

        protected override Task ProcessMessageAsync(WebSocketMessageQueueItem<WebSocketServerConnection> item, CancellationToken cancellationToken)
        {
            return RedirectAsync(item.Data, new WebSocketMessagePayload(item.MessageType, item.EndOfMessage), cancellationToken);
        }

        private Task RedirectAsync(ArraySegment<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken)
        {
            try
            {
                var message = Encoding.UTF8.GetString(buffer);
                var jObj = JsonConvert.DeserializeObject<JObject>(message);

                if (jObj == null)
                    return Task.CompletedTask;

                var jSyncToken = jObj.GetValue("ConnectionId");
                var syncTokenStr = jSyncToken?.Value<string>();

                if (!Guid.TryParse(syncTokenStr, out var syncToken))
                    return Task.CompletedTask;

                var connection = _connectionResolver.Resolve(syncToken);
                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jObj));

                return connection.SendAsync(messageBytes, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
    }
}
