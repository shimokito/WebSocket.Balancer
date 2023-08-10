using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Websocket.Tcp.Proxy.ConnectionResolver;
using Websocket.Tcp.Proxy.Connections.Models;

namespace Websocket.Tcp.Proxy.Connections.Queue.Client
{
    public class ClientWebSocketMessageQueue : WebSocketMessageQueue<WebSocketClientConnection>
    {
        private readonly ServerConnectionResolver _connectionResolver;

        public ClientWebSocketMessageQueue(ServerConnectionResolver connectionResolver)
        {
            _connectionResolver = connectionResolver;
        }

        protected override async Task ProcessMessageAsync(WebSocketMessageQueueItem<WebSocketClientConnection> item, CancellationToken cancellationToken)
        {
            var sender = item.Connection;
            try
            {
                await RedirectAsync(item.Connection.Id, item.Data, new WebSocketMessagePayload(item.MessageType, item.EndOfMessage), cancellationToken);
            }
            catch (Exception e)
            {
                await sender.SendAsync(Encoding.UTF8.GetBytes(e.Message), new WebSocketMessagePayload(System.Net.WebSockets.WebSocketMessageType.Text, true), cancellationToken);
            }
        }

        private Task RedirectAsync(Guid connectionId, ArraySegment<byte> buffer, WebSocketMessagePayload payload, CancellationToken cancellationToken)
        {
            try
            {
                var message = Encoding.UTF8.GetString(buffer);
                var jObj = JsonConvert.DeserializeObject<JObject>(message);

                if (jObj == null)
                    return Task.CompletedTask;

                var jSyncToken = jObj.GetValue("ProjectName");
                var syncToken = jSyncToken?.Value<string>();

                if (syncToken == null)
                    return Task.CompletedTask;

                var connection = _connectionResolver.Resolve(syncToken);

                jObj.Add("ConnectionId", connectionId);
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
