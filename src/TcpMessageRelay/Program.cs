using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TcpLibrary.Config;

class Program
{
    static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddSingleton<IClientService>(sp => new TcpClientService(new TcpConfig(ServerIp: "192.168.1.101", Port: 5000), sp.GetRequiredService<ILogger<TcpClientService>>()))
            .AddSingleton<IClientService>(sp => new TcpClientService(new TcpConfig(ServerIp: "192.168.1.102", Port: 6000), sp.GetRequiredService<ILogger<TcpClientService>>()))
            .AddSingleton<IEventHandler<string>, LoggingMessageHandler>()
            .AddSingleton<IEventHandler<string>, ForwardingMessageHandler>()
            .AddSingleton<ITcpMessageRelay, TcpMessageRelay>()
            .BuildServiceProvider();

        var relay = serviceProvider.GetRequiredService<ITcpMessageRelay>();
        
        bool relayStarted = await relay.StartRelayAsync();
        if (!relayStarted)
        {
            Console.WriteLine("❌ TCP Relay failed to start. Exiting...");
            return;
        }

        Console.WriteLine("✅ TCP Relay is running. Press [Enter] to stop.");
        Console.ReadLine();

        await relay.StopRelayAsync();
        Console.WriteLine("🚫 TCP Relay stopped.");
    }
}
