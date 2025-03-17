using Microsoft.Extensions.Logging;
using TcpLibrary.Clients;
using TcpLibrary.Exceptions;
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
        logger.LogInformation("Starting TCP Relay Service...");

        if (!await TryConnectClient(vehicleClient, "Vehicle")) return false;
        if (!await TryConnectClient(backendClient, "Backend"))
        {
            await SafeDisconnectClient(vehicleClient, "Vehicle");
            return false;
        }

        SetupEventHandlers();

        logger.LogInformation("TCP Message Relay Started.");
        return true;
    }

    /// <summary>
    /// Stops the relay (disconnects both clients).
    /// </summary>
    public async Task StopRelayAsync()
    {
        logger.LogInformation("Stopping TCP Message Relay...");

        if (_vehicleHandler != null)
        {
            vehicleClient.MessageReceived -= _vehicleHandler;
            _vehicleHandler = null;
        }
        if (_backendHandler != null)
        {
            backendClient.MessageReceived -= _backendHandler;
            _backendHandler = null;
        }

        await SafeDisconnectClient(vehicleClient, "Vehicle");
        await SafeDisconnectClient(backendClient, "Backend");

        logger.LogInformation("TCP Message Relay Stopped.");
    }


    /// <summary>
    /// Attempts to connect a client and handles failures gracefully.
    /// </summary>
    private async Task<bool> TryConnectClient(IClientService client, string clientName)
    {
        try
        {
            bool connected = await client.ConnectAsync();
            if (!client.IsConnected)
            {
                logger.LogError("{ClientName} connection failed.", clientName);
                return false;
            }
            return true;
        }
        catch (TcpConnectionException ex)
        {
            logger.LogWarning("[Handled] {ClientName} connection error: {ErrorMessage}", clientName, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sets up event handlers for message forwarding.
    /// </summary>
    private void SetupEventHandlers()
    {
        _vehicleHandler = async (sender, msg) => await HandleMessageAsync(msg, vehicleToBackendHandler, "Backend");
        _backendHandler = async (sender, msg) => await HandleMessageAsync(msg, backendToVehicleHandler, "Vehicle");

        vehicleClient.MessageReceived += _vehicleHandler;
        backendClient.MessageReceived += _backendHandler;
    }

    /// <summary>
    /// Handles a forwarded message and gracefully handles errors.
    /// </summary>
    private async Task HandleMessageAsync(byte[] msg, IEventHandler<byte[]> handler, string target)
    {
        if (msg == null || msg.Length == 0)
        {
            logger.LogWarning("[Handled] Ignoring empty message to {Target}.", target);
            return;
        }

        try
        {
            await handler.HandleAsync(msg);
        }
        catch (TcpSendException ex)
        {
            logger.LogWarning("[Handled] Failed to send message to {Target}: {ErrorMessage}", target, ex.Message);
        }
    }


    /// <summary>
    /// Safely disconnects a client and handles disconnection errors.
    /// </summary>
    private async Task SafeDisconnectClient(IClientService client, string clientName)
    {
        try
        {
            await client.DisconnectAsync();
        }
        catch (TcpClientException ex)
        {
            logger.LogWarning("[Handled] Error disconnecting {ClientName}: {ErrorMessage}", clientName, ex.Message);
        }
    }
}
