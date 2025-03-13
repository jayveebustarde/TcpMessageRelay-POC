using Microsoft.Extensions.Logging;
using TcpMessageRelay.Clients;

namespace TcpMessageRelay.Relay;

public class TcpRelayService(
    IVehicleClient vehicleClient,
    IBackendClient backendClient,
    IEventHandler<byte[]> vehicleToBackendHandler,
    IEventHandler<byte[]> backendToVehicleHandler,
    ILogger<TcpRelayService> logger) : ITcpRelayService
{
    private EventHandler<byte[]>? _vehicleHandler;
    private EventHandler<byte[]>? _backendHandler;

    /// <summary>
    /// Starts the relay (only if vehicle connects first).
    /// </summary>
    public async Task<bool> StartRelayAsync()
    {
        logger.LogInformation("Waiting for vehicle connection...");

        bool vehicleConnected = await vehicleClient.ConnectAsync();
        if (!vehicleClient.IsConnected)
        {
            logger.LogError("Vehicle connection failed. Backend connection will NOT start.");
            return false;
        }

        logger.LogInformation("Vehicle connected. Now attempting backend connection...");
        bool backendConnected = await backendClient.ConnectAsync();

        if (!backendClient.IsConnected)
        {
            logger.LogError("Backend connection failed. Closing vehicle connection...");
            await vehicleClient.DisconnectAsync();
            return false;
        }

        // Store handlers to allow unsubscription
        _vehicleHandler = async (sender, msg) => await vehicleToBackendHandler.HandleAsync(msg);
        _backendHandler = async (sender, msg) => await backendToVehicleHandler.HandleAsync(msg);

        // Ensure event handlers are set when relay starts
        vehicleClient.MessageReceived += _vehicleHandler;
        backendClient.MessageReceived += _backendHandler;

        logger.LogInformation("TCP Message Relay Started.");
        return true;
    }

    /// <summary>
    /// Stops the relay (disconnects both clients).
    /// </summary>
    public async Task StopRelayAsync()
    {
        logger.LogInformation("Stopping TCP Message Relay...");

        // Unsubscribe from events
        vehicleClient.MessageReceived -= _vehicleHandler;
        backendClient.MessageReceived -= _backendHandler;

        await vehicleClient.DisconnectAsync();
        await backendClient.DisconnectAsync();

        logger.LogInformation("TCP Message Relay Stopped.");
    }
}
