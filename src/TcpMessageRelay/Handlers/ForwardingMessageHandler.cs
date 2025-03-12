public class ForwardingMessageHandler : IEventHandler<string>
{
    private readonly IClientService _backendClient;

    public ForwardingMessageHandler(IClientService backendClient)
    {
        _backendClient = backendClient;
    }

    public async Task HandleAsync(string message)
    {
        await _backendClient.SendMessageAsync(message);
    }
}
