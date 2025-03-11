using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class LoggingMessageHandler : IEventHandler<string>
{
    private readonly ILogger<LoggingMessageHandler> _logger;

    public LoggingMessageHandler(ILogger<LoggingMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(string message)
    {
        _logger.LogInformation($"📝 Logging message: {message}");
        await Task.CompletedTask;
    }
}
