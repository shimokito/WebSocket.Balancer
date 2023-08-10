using HttpMachine;
using Websocket.Tcp.Extensions.HttpParser.Factory;
using Websocket.Tcp.Extensions.HttpParser.Interface;

namespace Websocket.Tcp.Extensions.HttpParser
{
    public class HttpBinaryRequestParser : IHttpBinaryRequestParser, IDisposable
    {
        private readonly HttpCombinedParser _parser;
        private readonly CombinedFactoryDelegate _factoryDelegate;

        public HttpBinaryRequestParser(IHttpRequestFactoryDelegate factoryDelegate, IHttpRequestFactory factory)
        {
            _factoryDelegate = new CombinedFactoryDelegate(factoryDelegate, factory);
            _parser = new HttpCombinedParser(_factoryDelegate);
        }

        public void Parse(ArraySegment<byte> bytes)
        {
            _parser.Execute(bytes);
        }

        public void Dispose()
        {
            _factoryDelegate.Dispose();
            _parser.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}