using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading.Channels;
using Websocket.Tcp.Server.Websocket.Interface;

namespace Websocket.Tcp.Server.Websocket
{
    public class WebsocketConnectionBuffer : IWebsocketConnectionConsumer, IWebsocketConnectionProducer
    {
        private const int UnboundBackLog = -1;

        private readonly Channel<WebSocketContext> _connections;

        public WebsocketConnectionBuffer()
            : this(UnboundBackLog, false, false)
        {

        }
        public WebsocketConnectionBuffer(int backLog)
            : this(backLog, false, false)
        {

        }
        private WebsocketConnectionBuffer(int backLog, bool singleReader, bool singleWriter)
        {
            if (backLog == UnboundBackLog)
            {
                _connections = Channel.CreateUnbounded<WebSocketContext>(new UnboundedChannelOptions
                {
                    SingleReader = singleReader,
                    SingleWriter = singleWriter,
                    AllowSynchronousContinuations = false
                });
            }
            else
            {
                if (backLog == 0)
                    throw new ArgumentOutOfRangeException(nameof(backLog));

                _connections = Channel.CreateBounded<WebSocketContext>(new BoundedChannelOptions(backLog)
                {
                    SingleReader = singleReader,
                    SingleWriter = singleWriter,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
            }
        }

        public void Next(WebSocketContext connection)
        {
            _connections.Writer.TryWrite(connection);
        }

        public ValueTask<WebSocketContext> ReadAsync(CancellationToken cancellationToken = default)
        {
            return _connections.Reader.ReadAsync(cancellationToken);
        }

        public bool TryTake([MaybeNullWhen(false)] out WebSocketContext connection)
        {
            return _connections.Reader.TryRead(out connection);
        }

        public ValueTask<bool> WaitConnectionAsync(CancellationToken cancellationToken = default)
        {
            return _connections.Reader.WaitToReadAsync(cancellationToken);
        }

        public Task CloseAsync()
        {
            _connections.Writer.Complete(new ChannelClosedException());
            return _connections.Reader.Completion;
        }

        public void Close()
        {
            CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
