using System.Net.WebSockets;
using System.Text;
using Websocket.Tcp.Proxy.ConnectionResolver;
using Websocket.Tcp.Proxy.Connections.Core;
using Websocket.Tcp.Proxy.Connections.Queue;
using Websocket.Tcp.Proxy.Connections.Queue.Server;
using Websocket.Tcp.Proxy.Options;

namespace Websocket.Tcp.Proxy.Connections
{
    public class WebSocketServerConnection : WebSocketConnection
    {
        private readonly ServerWebSocketMessageQueue _messageQueue;

        public WebSocketServerConnection(WebsocketTunnelOptions options, ClientConnectionResolver resolver)
            : base(CreateWebSocket(options))
        {
            _messageQueue = new ServerWebSocketMessageQueue(resolver);
        }

        private static WebSocket CreateWebSocket(WebsocketTunnelOptions tunnelOptions)
        {
            if (tunnelOptions.CountConnections <= 0)
                throw new ArgumentOutOfRangeException(nameof(tunnelOptions.CountConnections), "Count connection less then zero.");

            return new MultiConnectedWebSocket(tunnelOptions.ServerUri, tunnelOptions.CountConnections, tunnelOptions.ReconnectionPolicy, tunnelOptions.SendingMode);
        }

        protected override void StartListening(CancellationToken cancellationToken)
        {
            _messageQueue.Start(cancellationToken);
            base.StartListening(cancellationToken);
        }

        protected override async Task ListenAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(webSocket, cancellationToken);

            var buffer = new byte[4096];
            while (true)
            {
                var ms = new MemoryStream();
                try
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                        ms.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    var bytes = ms.ToArray();
                    await _messageQueue.EnqueueAsync(new WebSocketMessageQueueItem<WebSocketServerConnection>(this, bytes, result.MessageType, result.EndOfMessage), cancellationToken);
                }
                catch (Exception ex)
                {
                    await webSocket.SendAsync(Encoding.UTF8.GetBytes(ex.Message), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
                }
                finally
                {
                    ms.Dispose();
                }
            }
        }

        private ValueTask<WebSocket> EnsureConnectedAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            if (webSocket is not MultiConnectedWebSocket client)
            {
                return ValueTask.FromException<WebSocket>(new InvalidOperationException("Unexpected websocket."));
            }

            return new ValueTask<WebSocket>(client.ConnectAsync(cancellationToken)
                .ContinueWith((_, state) => (WebSocket)state!, client, cancellationToken));
        }
    }
}
