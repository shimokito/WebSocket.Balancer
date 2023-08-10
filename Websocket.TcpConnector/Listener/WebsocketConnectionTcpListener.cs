using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Websocket.Tcp.Extensions.HttpParser;
using Websocket.Tcp.Server.Exceptions;
using Websocket.Tcp.Server.Listener.Options;
using Websocket.Tcp.Server.Utils;
using Websocket.Tcp.Server.Utils.HttpFactory;
using Websocket.Tcp.Server.Websocket.Context;
using Websocket.Tcp.Server.Websocket.Interface;

namespace Websocket.Tcp.Server.Listener
{
    public class WebsocketConnectionTcpListener : TcpConnectionListener
    {
        private const int DefaultBufferSize = 4 * 1024;
        private readonly IWebsocketConnectionConsumer _consumer;
        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        protected new WebsocketListenerOptions Options => (WebsocketListenerOptions)base.Options;

        public WebsocketConnectionTcpListener(IPEndPoint ipEndPoint, IWebsocketConnectionConsumer consumer, WebsocketListenerOptions? options = null)
            : base(ipEndPoint, options)
        {
            _consumer = consumer;
        }
        public WebsocketConnectionTcpListener(TcpListener tcpListener, IWebsocketConnectionConsumer consumer, WebsocketListenerOptions? options = null)
            : base(tcpListener, options)
        {
            _consumer = consumer;
        }
        public WebsocketConnectionTcpListener(IPAddress ipAddress, int port, IWebsocketConnectionConsumer consumer, WebsocketListenerOptions? options = null)
            : base(ipAddress, port, options)
        {
            _consumer = consumer;
        }

        public async Task<WebSocketContext> AccepWebSocketAsync(CancellationToken cancellationToken)
        {
            var tcpClient = await AcceptTcpClientAsync(cancellationToken);
            var networkStream = tcpClient.GetStream();
            return await AcceptWebsocketAsyncCore(networkStream, cancellationToken);
        }

        protected override async Task OnTcpClientAcceptedAsync(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            var networkStream = tcpClient.GetStream();
            bool isAccepted = false;
            try
            {
                isAccepted = await ConsumeWebsocketAsyncCore(networkStream, cancellationToken);
            }
            finally
            {
                if (!isAccepted)
                    tcpClient.Dispose();
            }
        }

        protected override void OnTcpClientError(TcpClient tcpClient, Exception exception)
        {
            base.OnTcpClientError(tcpClient, exception);
        }

        private async Task<bool> ConsumeWebsocketAsyncCore(NetworkStream networkStream, CancellationToken cancellationToken)
        {
            try
            {
                using var httpRequest = await ReadHttpRequestAsync(networkStream, cancellationToken);

                var acceptSubProtocol = Options.SubProtocol;
                if (!WebsocketHandshakeHelper.ValidateWebsocketRequest(httpRequest) ||
                    !WebsocketHandshakeHelper.ContainsSubProtocol(httpRequest, acceptSubProtocol))
                {
                    await networkStream.WriteAsync(WebsocketHandshakeHelper.BadRequest(), cancellationToken);
                    return false;
                }

                var handshake = WebsocketHandshakeHelper.Handshake(httpRequest, acceptSubProtocol);
                var handshakeTask = networkStream.WriteAsync(handshake, cancellationToken);

                var websocket = WebSocket.CreateFromStream(networkStream, new WebSocketCreationOptions
                {
                    IsServer = true,
                    SubProtocol = acceptSubProtocol,
                    KeepAliveInterval = Options.KeepAliveInterval
                });

                var context = TcpListenerWebsocketContext.Create(websocket, httpRequest);
                _consumer.Next(context);

                await handshakeTask.ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await networkStream.WriteAsync(WebsocketHandshakeHelper.BadRequest(), cancellationToken);
                throw new WebsocketHandshakeException(ex);
            }
        }

        private async Task<WebSocketContext> AcceptWebsocketAsyncCore(NetworkStream networkStream, CancellationToken cancellationToken)
        {
            try
            {
                using var httpRequest = await ReadHttpRequestAsync(networkStream, cancellationToken);

                var acceptSubProtocol = Options.SubProtocol;
                if (!WebsocketHandshakeHelper.ValidateWebsocketRequest(httpRequest) ||
                    !WebsocketHandshakeHelper.ContainsSubProtocol(httpRequest, acceptSubProtocol))
                {
                    await networkStream.WriteAsync(WebsocketHandshakeHelper.BadRequest(), cancellationToken);
                    throw new WebSocketException("Invalid handshake request.");
                }

                var handshake = WebsocketHandshakeHelper.Handshake(httpRequest, acceptSubProtocol);
                var handshakeTask = networkStream.WriteAsync(handshake, cancellationToken);

                var websocket = WebSocket.CreateFromStream(networkStream, new WebSocketCreationOptions
                {
                    IsServer = true,
                    SubProtocol = acceptSubProtocol,
                    KeepAliveInterval = Options.KeepAliveInterval
                });

                var context = TcpListenerWebsocketContext.Create(websocket, httpRequest);
                await handshakeTask.ConfigureAwait(false);
                return context;
            }
            catch (Exception ex)
            {
                await networkStream.WriteAsync(WebsocketHandshakeHelper.BadRequest(), cancellationToken);
                throw new WebsocketHandshakeException(ex);
            }
        }

        private async Task<HttpRequestMessage> ReadHttpRequestAsync(NetworkStream networkStream, CancellationToken cancellationToken, int bufferSize = DefaultBufferSize)
        {
            using var factoryDelegate = new WebsocketHandshakeHttpRequestFactoryDelegate();
            using var parser = new HttpBinaryRequestParser(factoryDelegate, factoryDelegate);

            int count = bufferSize;
            using var buffer = _bufferPool.RentBuffer(count);
            do
            {
                var readBytes = await networkStream.ReadAsync(buffer.AsMemory(), cancellationToken);
                parser.Parse(new ArraySegment<byte>(buffer.Data, 0, readBytes));

            } while (networkStream.DataAvailable && !factoryDelegate.IsHttpHandle);

            if (!factoryDelegate.IsHttpHandle)
                throw new InvalidOperationException("Unable to read http request websocket handshake.");

            return factoryDelegate.BuildHttpRequest()!;
        }
    }
}