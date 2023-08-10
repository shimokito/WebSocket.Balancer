using System.Threading.Channels;
using Websocket.Tcp.Proxy.Connections.Core.MessagePool.Models;

namespace Websocket.Tcp.Proxy.Connections.Core.MessagePool
{
    internal class WebSocketMessageHeap : IDisposable
    {
        private readonly Channel<SendMessage> _sendMessages;
        private readonly Channel<ReceiveMessage> _receivedMessages;

        public WebSocketMessageHeap()
        {
            _sendMessages = Channel.CreateUnbounded<SendMessage>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            });
            _receivedMessages = Channel.CreateUnbounded<ReceiveMessage>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        }


        public ValueTask<ReceiveMessage> ReadReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return _receivedMessages.Reader.ReadAsync(cancellationToken);
        }

        public ValueTask<SendMessage> ReadSendMessageAsync(CancellationToken cancellationToken)
        {
            return _sendMessages.Reader.ReadAsync(cancellationToken);
        }

        public void Write(WebSocketHeapMessage message)
        {
            switch (message)
            {
                case SendMessage send:
                    _sendMessages.Writer.TryWrite(send);
                    break;
                case ReceiveMessage receive:
                    _receivedMessages.Writer.TryWrite(receive);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message));
            }
        }

        public ValueTask WriteAsync(WebSocketHeapMessage message, CancellationToken cancellationToken = default)
        {
            switch (message)
            {
                case SendMessage send:
                    return _sendMessages.Writer.WriteAsync(send, cancellationToken);
                case ReceiveMessage receive:
                    return _receivedMessages.Writer.WriteAsync(receive, cancellationToken);
                default:
                    return ValueTask.FromException(new ArgumentOutOfRangeException(nameof(message)));
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
