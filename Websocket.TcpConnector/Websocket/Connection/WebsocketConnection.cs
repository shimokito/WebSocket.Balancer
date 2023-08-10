using System.Net.WebSockets;

namespace Websocket.Tcp.Server.Websocket.Connection
{
    public class WebsocketConnection
    {
        private readonly WebSocket _internalWebsocket;

        internal WebsocketConnection(WebSocket internalWebsocket)
        {
            _internalWebsocket = internalWebsocket;
        }

        public void Start()
        {
            _ = Task.Run(() => ListenAsync(CancellationToken.None));
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            ArraySegment<byte> buffer = new byte[4 * 1024];
            WebSocket client = _internalWebsocket;
            do
            {
                byte[]? resultArrayWithTrailing = null;
                int resultArraySize = 0;
                //bool isResultArrayCloned = false;
                MemoryStream? ms = null;
                WebSocketReceiveResult result;

                do
                {
                    result = await client.ReceiveAsync(buffer, cancellationToken);
                    byte[]? array = buffer.Array;
                    int count = result.Count;

                    if (resultArrayWithTrailing == null)
                    {
                        resultArraySize += count;
                        resultArrayWithTrailing = array;
                        //isResultArrayCloned = false;
                    }
                    else if (array != null)
                    {
                        if (ms == null)
                        {
                            ms = new MemoryStream();
                            ms.Write(resultArrayWithTrailing, 0, resultArraySize);
                        }

                        ms.Write(array, buffer.Offset, count);
                    }

                } while (!result.EndOfMessage);

            } while (client.State == WebSocketState.Open);
        }

        public Task SendAsync(string message)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(byte[] binaryMessage)
        {
            throw new NotImplementedException();
        }
    }
}
