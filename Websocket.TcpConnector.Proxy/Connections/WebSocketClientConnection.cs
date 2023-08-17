using System.Net.WebSockets;
using System.Text;
using Websocket.Tcp.Proxy.ConnectionResolver;
using Websocket.Tcp.Proxy.Connections.Queue;
using Websocket.Tcp.Proxy.Connections.Queue.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Websocket.Tcp.Proxy.Connections
{
    public class WebSocketClientConnection : WebSocketConnection
    {
        private readonly Guid _connectionId;
        private readonly ClientWebSocketMessageQueue _messageQueue;
        private IUnsubcriber<WebSocketClientConnection>? _unsubscriber;

        public Guid Id => _connectionId;

        public WebSocketClientConnection(Guid connectionId, WebSocket webSocket, ServerConnectionResolver connectionResolver)
            : base(webSocket)
        {
            _messageQueue = new ClientWebSocketMessageQueue(connectionResolver);
            _connectionId = connectionId;
        }

        protected override void StartListening(CancellationToken cancellationToken)
        {
            base.StartListening(cancellationToken);
            _messageQueue.Start(cancellationToken);
        }

        protected override async Task ListenAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            if (webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket connection not openned.");

            ArraySegment<byte> buffer = new byte[4096];
            while (true)
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        WebSocketReceiveResult result;
                        MemoryStream? ms = null;
                        byte[]? bufferWithTrailing = null;
                        int count = 0;

                        //Первая иттерация получения сообщения
                        do
                        {
                            result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                            byte[]? array = buffer.Array;

                            if (array != null)
                            {
                                bufferWithTrailing = array;
                                count += result.Count;
                            }

                        } while (!result.EndOfMessage && result.Count == 0);

                        //Проверка на окончание сообщения.
                        if (!result.EndOfMessage)
                        {
                            //Копируем буфер и заполняем в стрим.
                            bufferWithTrailing = bufferWithTrailing!.ToArray();
                            ms ??= new MemoryStream();
                            ms.Write(bufferWithTrailing, 0, count);

                            //Считываем сообщение до конца.
                            do
                            {
                                result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                byte[]? array = buffer.Array;

                                if (array != null)
                                {
                                    ms.Write(array, 0, result.Count);
                                    count += result.Count;
                                }

                            } while (!result.EndOfMessage);
                        }

                        //Если сообщение закрывающее вебсокет, передаём ивент.
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new NotImplementedException("Close hanshake not implemented.");
                        }

                        //Копируем результат.
                        ArraySegment<byte> bytes;
                        if (ms == null)
                        {
                            bytes = new ArraySegment<byte>(new byte[count]);
                            Array.Copy(bufferWithTrailing!, 0, bytes.Array!, 0, count);
                        }
                        else
                        {
                            bytes = ms.ToArray();
                        }

                        //Очищаем стрим если нужно.
                        ms?.Dispose();

                        //Отправляем ивент сообщения.
                        var message = Encoding.UTF8.GetString(bytes.Slice(0, count));
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    //try
                    //{
                    //    WebSocketReceiveResult result;
                    //    MemoryStream? ms = null;
                    //    byte[]? bufferWithTrailing = null;
                    //    int count = 0;

                    //    do
                    //    {
                    //        result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    //        byte[]? array = buffer.Array;
                    //        if (bufferWithTrailing == null)
                    //        {
                    //            bufferWithTrailing = array;
                    //            count += result.Count;
                    //        }
                    //        else if (array != null)
                    //        {
                    //            if (ms == null)
                    //            {
                    //                ms = new MemoryStream();
                    //                ms.Write(bufferWithTrailing, 0, count);
                    //            }

                    //            ms.Write(array, 0, result.Count);
                    //            count += result.Count;
                    //        }

                    //    } while (!result.EndOfMessage);

                    //    if (bufferWithTrailing == null)
                    //        continue;

                    //    ArraySegment<byte> bytes;
                    //    if (ms == null)
                    //    {
                    //        bytes = new ArraySegment<byte>(new byte[count]);
                    //        Array.Copy(bufferWithTrailing, 0, bytes.Array!, 0, count);
                    //    }
                    //    else
                    //    {
                    //        bytes = ms.ToArray();
                    //    }

                    //    ms?.Dispose();

                    //    if (result.MessageType == WebSocketMessageType.Close)
                    //    {
                    //        await CloseOutputAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, cancellationToken).ConfigureAwait(false);
                    //        _unsubscriber?.Dispose();
                    //        return;
                    //    }
                    //    var slicedBuffer = bytes.Slice(0, count);
                    //    await _messageQueue.EnqueueAsync(new WebSocketMessageQueueItem<WebSocketClientConnection>(this, slicedBuffer, result.MessageType, true), cancellationToken);
                    //}
                    //catch (Exception ex)
                    //{
                    //    await webSocket.SendAsync(Encoding.UTF8.GetBytes(ex.Message), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
                    //}
                }
            }
        }

        internal void Register(IUnsubcriber<WebSocketClientConnection> unsubcriber)
        {
            if (_unsubscriber != null)
                throw new InvalidOperationException("Unsubsriber alredy registered.");

            _unsubscriber = unsubcriber;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _unsubscriber?.Dispose();
            }
        }
    }
}
