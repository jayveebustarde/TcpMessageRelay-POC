using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddSingleton(new TcpConfig { ServerIp = "192.168.1.101", Port = 5000 })  // Vehicle
            .AddSingleton(new TcpConfig { ServerIp = "192.168.1.102", Port = 6000 })  // Backend
            .AddSingleton<ITcpClientService, TcpClientService>()
            .AddSingleton<IEventHandler<string>, LoggingMessageHandler>()
            .AddSingleton<IEventHandler<string>, ForwardingMessageHandler>()
            .BuildServiceProvider();

        var vehicleClient = serviceProvider.GetRequiredService<ITcpClientService>();
        var backendClient = serviceProvider.GetRequiredService<ITcpClientService>();

        var handlers = serviceProvider.GetServices<IEventHandler<string>>();
        vehicleClient.MessageReceived += async (sender, message) =>
        {
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(message);
            }
        };

        await vehicleClient.ConnectAsync();
        await backendClient.ConnectAsync();
    }
}
