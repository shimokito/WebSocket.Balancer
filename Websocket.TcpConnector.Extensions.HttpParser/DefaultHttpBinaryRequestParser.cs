using HttpMachine;
using Websocket.Tcp.Extensions.HttpParser.Factory;
using Websocket.Tcp.Extensions.HttpParser.Interface;

namespace Websocket.Tcp.Extensions.HttpParser
{
    public class DefaultHttpBinaryRequestParser : IHttpBinaryRequestParser, IDisposable
    {
        private readonly HttpCombinedParser _parser;
        private readonly DefaultHttpFactory _factory;

        public bool IsParsed => _factory.IsHttpHandled;

        public DefaultHttpBinaryRequestParser()
        {
            _factory = new DefaultHttpFactory();
            _parser = new HttpCombinedParser(_factory);
        }

        public void Parse(ArraySegment<byte> bytes)
        {
            _parser.Execute(bytes);
        }

        public HttpRequestMessage Build(bool resetFactory = true)
        {
            if (!_factory.IsHttpHandled)
                throw new InvalidOperationException("Http not handled.");

            if (resetFactory)
                _factory.Reset();

            return _factory.BuildHttpRequest();
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}