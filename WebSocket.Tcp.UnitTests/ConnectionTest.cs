using System.Text;
using Websocket.Tcp.Proxy.Connections.Core;

namespace WebSocket.Tcp.UnitTests
{
    public class ConnectionTest : IAsyncLifetime
    {
        private readonly MultiConnectedWebSocket _webSocket;

        public ConnectionTest()
        {
            var uri = new Uri("wss://ws.postman-echo.com/raw");
            _webSocket = new MultiConnectedWebSocket(uri, 3, new ReconnectionPolicy());
        }

        [Fact]
        public async Task SendAsync()
        {
            var bytes = GetMessageBytes();
            var sendBytes = bytes.ToArray();

            for (var i = 0; i < 1000; i++)
            {
                await _webSocket.SendAsync(sendBytes, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            }

            for (var i = 0; i < 1000; i++)
            {
                Array.Clear(bytes);
                Assert.All(bytes, x => Assert.Equal(0, x));
                var receiveResult = await _webSocket.ReceiveAsync(bytes, default);
                Assert.Equal(bytes, sendBytes);
            }

            await Task.Delay(Timeout.Infinite);
        }

        [Fact]
        public void Send()
        {
            var bytes = GetMessageBytes();
            _webSocket.Send(bytes, System.Net.WebSockets.WebSocketMessageType.Text, true);
        }

        private byte[] GetMessageBytes()
        {
            var message = "Hello, world!";
            return Encoding.UTF8.GetBytes(message);
        }

        public async Task InitializeAsync()
        {
            await _webSocket.ConnectAsync(default);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}