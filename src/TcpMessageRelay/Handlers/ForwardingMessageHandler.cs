using Microsoft.Extensions.Logging;

public class ForwardingMessageHandler(IClientService targetClient, ILogger<ForwardingMessageHandler> logger) : IEventHandler<byte[]>
{
    private readonly IClientService _targetClient = targetClient;
    private readonly ILogger<ForwardingMessageHandler> _logger = logger;
    private static int _logCounter = 0;

    public async Task HandleAsync(byte[] message)
    {
        if (message == null || message.Length == 0)
        {
            _logger.LogWarning("Received an empty or null message. Skipping forwarding.");
            return;
        }

        if (!_targetClient.IsConnected)
        {
            _logger.LogWarning("Target client is not connected. Message not forwarded.");
            return;
        }

        try
        {
            await _targetClient.SendMessageAsync(message);

            if (++_logCounter % 100 == 0) // Log every 100 messages
            {
                _logger.LogInformation("Message forwarded successfully. Total forwarded: {Count}", _logCounter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forward message. Connection may have dropped.");
        }
    }
}
