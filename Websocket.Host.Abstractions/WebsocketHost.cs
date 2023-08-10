using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using Websocket.Tcp.Proxy;
using Websocket.Tcp.Server.Listener;
using Websocket.Tcp.Server.Listener.Options;
using Websocket.Tcp.Server.Websocket;
using Websocket.Tcp.Server.Websocket.Interface;

namespace Websocket.Tcp.Host.Abstractions
{
    public class WebsocketHost : IWebsocketHost
    {
        private WebsocketConnectionTcpListener? _listener;
        private readonly IPEndPoint _endPoint;
        private readonly WebsocketListenerOptions _options;
        private readonly ServiceCollection _serviceCollection;
        private IServiceProvider? _serviceProvider;


        private WebsocketHost(IPEndPoint IPEndPoint, WebsocketListenerOptions? options)
        {
            _endPoint = IPEndPoint;
            _options = options ?? new WebsocketListenerOptions();
            _serviceCollection = new ServiceCollection();
        }

        public static WebsocketHost Create(IPEndPoint IPEndPoint, WebsocketListenerOptions? options = null)
        {
            return new WebsocketHost(IPEndPoint, options ?? new WebsocketListenerOptions());
        }

        public IServiceCollection ServiceCollection => _serviceCollection;
        public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException();

        public void Build()
        {
            RegisterLifetime();
            RegisterProducer();

            _serviceProvider = _serviceCollection.BuildServiceProvider();
        }

        private void RegisterLifetime()
        {
            _serviceCollection.AddSingleton<IHostApplicationLifetime, Lifetime>();
        }

        private void RegisterProducer()
        {
            _serviceCollection.AddSingleton<WebsocketConnectionBuffer>();
            _serviceCollection.AddSingleton<IWebsocketConnectionProducer>(x => x.GetRequiredService<WebsocketConnectionBuffer>());
        }

        public void Dispose()
        {
            _listener?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await using var scope = Services.CreateAsyncScope();
            var consumer = scope.ServiceProvider.GetService<WebsocketConnectionBuffer>() ?? throw new InvalidOperationException("Connection consumer not provided.");

            Console.WriteLine("Start host on {0}", _endPoint);
            _listener = new WebsocketConnectionTcpListener(_endPoint, consumer, _options);
            _listener.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _listener?.Stop();
            var consumer = Services.GetRequiredService<WebsocketConnectionBuffer>();
            await consumer.CloseAsync();
        }

        private class Lifetime : IHostApplicationLifetime
        {
            private readonly CancellationTokenSource _cts;

            public Lifetime()
            {
                _cts = new CancellationTokenSource();
            }

            public CancellationToken ApplicationStarted => _cts.Token;

            public CancellationToken ApplicationStopping => _cts.Token;

            public CancellationToken ApplicationStopped => _cts.Token;

            public void StopApplication()
            {
                _cts.Cancel();
            }
        }
    }
}