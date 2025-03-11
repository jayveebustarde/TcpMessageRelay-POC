public interface ITcpClientService
{
    event EventHandler<string> MessageReceived;
    event EventHandler<string> OnDisconnect;
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task SendMessageAsync(string message);
}
