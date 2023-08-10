namespace Websocket.Tcp.Extensions.HttpParser.Interface
{
    public interface IHttpRequestFactoryDelegate : IHttpFactoryDelegate
    {
        void OnMethod(IHttpRequestFactory factory, string method);

        void OnRequestUri(IHttpRequestFactory factory, string requestUri);

        void OnPath(IHttpRequestFactory factory, string path);

        void OnFragment(IHttpRequestFactory factory, string fragment);

        void OnQueryString(IHttpRequestFactory factory, string queryString);
    }
}