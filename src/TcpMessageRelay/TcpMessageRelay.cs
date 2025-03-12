using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class TcpMessageRelay : ITcpMessageRelay
{
    private readonly IClientService _vehicleClient;
    private readonly IClientService _backendClient;
    private readonly ILogger<TcpMessageRelay> _logger;

    private bool _isRelayActive = false;

    public TcpMessageRelay(IClientService vehicleClient, IClientService backendClient, ILogger<TcpMessageRelay> logger)
    {
        _vehicleClient = vehicleClient;
        _backendClient = backendClient;
        _logger = logger;
    }

    /// <summary>
    /// Starts the relay (only if vehicle connects first).
    /// </summary>
    public async Task<bool> StartRelayAsync()
    {
        _logger.LogInformation("Waiting for vehicle connection...");

        bool vehicleConnected = await _vehicleClient.ConnectAsync();
        if (!vehicleConnected)
        {
            _logger.LogError("Vehicle connection failed. Backend connection will NOT start.");
            return false;
        }

        _logger.LogInformation("Vehicle connected. Now attempting backend connection...");
        bool backendConnected = await _backendClient.ConnectAsync();

        if (!backendConnected)
        {
            _logger.LogError("Backend connection failed. Closing vehicle connection...");
            await _vehicleClient.DisconnectAsync();
            return false;
        }

        _isRelayActive = true;

        // Ensure event handlers are set when relay starts
        _vehicleClient.MessageReceived += async (sender, msg) => await RelayMessage(msg, _backendClient);
        _backendClient.MessageReceived += async (sender, msg) => await RelayMessage(msg, _vehicleClient);

        _logger.LogInformation("TCP Message Relay Started.");
        return true;
    }

    /// <summary>
    /// Stops the relay (disconnects both clients).
    /// </summary>
    public async Task StopRelayAsync()
    {
        _logger.LogInformation("Stopping TCP Message Relay...");
        _isRelayActive = false;

        // Unsubscribe from events
        _vehicleClient.MessageReceived -= async (sender, msg) => await RelayMessage(msg, _backendClient);
        _backendClient.MessageReceived -= async (sender, msg) => await RelayMessage(msg, _vehicleClient);

        await _vehicleClient.DisconnectAsync();
        await _backendClient.DisconnectAsync();

        _logger.LogInformation("TCP Message Relay Stopped.");
    }

    /// <summary>
    /// Relays messages from one connection to the other.
    /// </summary>
    private async Task RelayMessage(string message, IClientService targetClient)
    {
        if (!_isRelayActive)
        {
            _logger.LogWarning("Relay is inactive. Message not forwarded.");
            return;
        }

        _logger.LogInformation($"Relaying message: {message}");
        await targetClient.SendMessageAsync(message);
    }
}
