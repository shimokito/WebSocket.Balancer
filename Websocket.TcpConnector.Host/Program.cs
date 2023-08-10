using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using Websocket.Tcp.Host.Abstractions;
using Websocket.Tcp.Proxy;
using Websocket.Tcp.Proxy.Options;

namespace Websocket.Tcp.Host
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = WebsocketHost.Create(new IPEndPoint(IPAddress.Any, 5033));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"), false)
                .Build();

            host.ServiceCollection.Configure<WebsocketBalancerOptions>(configuration.GetSection(nameof(WebsocketBalancerOptions)));
            host.ServiceCollection.AddSingleton<WebsocketBalancerConnectionConsumer>();

            host.Build();

            using (var scope = host.Services.CreateScope())
            {
                var proxy = scope.ServiceProvider.GetRequiredService<WebsocketBalancerConnectionConsumer>();
                proxy.Start(CancellationToken.None);
            }

            host.Run();
        }
    }
}