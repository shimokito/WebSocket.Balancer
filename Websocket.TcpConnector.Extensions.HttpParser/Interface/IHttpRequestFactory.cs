namespace Websocket.Tcp.Extensions.HttpParser.Interface
{
    public interface IHttpRequestFactory
    {
        HttpRequestMessage? BuildHttpRequest();
    }

    public interface IHttpResponseFactory
    {
        HttpResponseMessage? BuildHttpResponse();
    }
}