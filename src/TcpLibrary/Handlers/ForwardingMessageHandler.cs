public class ForwardingMessageHandler : IEventHandler<string>
{
    private readonly ITcpClientService _backendClient;

    public ForwardingMessageHandler(ITcpClientService backendClient)
    {
        _backendClient = backendClient;
    }

    public async Task HandleAsync(string message)
    {
        await _backendClient.SendMessageAsync(message);
    }
}
