using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TcpLibrary.Clients;
using TcpLibrary.Config;
using TcpMessageRelay.Clients;
using TcpMessageRelay.Handlers;
using TcpMessageRelay.Relay;

class Program
{
    static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddSingleton(sp => (IVehicleClient)new TcpClientService(new TcpConfig(ServerIp: "192.168.1.101", Port: 5000), sp.GetRequiredService<ILogger<TcpClientService>>()))
            .AddSingleton(sp => (IBackendClient)new TcpClientService(new TcpConfig(ServerIp: "192.168.1.102", Port: 6000), sp.GetRequiredService<ILogger<TcpClientService>>()))
            .AddSingleton<IEventHandler<byte[]>, ForwardingMessageHandler>()
            .AddScoped<ITcpRelayService, TcpRelayService>()
            .BuildServiceProvider();

        var relay = serviceProvider.GetRequiredService<ITcpRelayService>();
        
        bool relayStarted = await relay.StartRelayAsync();
        if (!relayStarted)
        {
            Console.WriteLine("TCP Relay failed to start. Exiting...");
            return;
        }

        Console.WriteLine("TCP Relay is running. Press [Enter] to stop.");
        Console.ReadLine();

        await relay.StopRelayAsync();
        Console.WriteLine("TCP Relay stopped.");
    }
}
