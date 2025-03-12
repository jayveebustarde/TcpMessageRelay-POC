using Microsoft.Extensions.Logging.Abstractions;
using TcpLibrary.Config;

public class TcpClientServiceTests
{
    private readonly TcpConfig _config = new(
        ServerIp: "127.0.0.1",
        Port: 5000,
        MaxRetries: 3,
        RetryDelayMs: 1000
    );

    [Fact]
    public async Task ConnectAsync_ShouldReturnTrue_WhenConnectionSuccessful()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var tcpClient = new TcpClientService(_config, mockLogger);

        bool result = await tcpClient.ConnectAsync();

        Assert.True(result, "TCP Client should successfully connect.");
        await tcpClient.DisconnectAsync();
    }

    [Fact]
    public async Task ConnectAsync_ShouldReturnFalse_WhenConnectionFails()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var failingConfig = new TcpConfig(ServerIp: "192.168.1.200", Port: 9999, MaxRetries: 2);
        var tcpClient = new TcpClientService(failingConfig, mockLogger);

        bool result = await tcpClient.ConnectAsync();

        Assert.False(result, "TCP Client should fail connection after retries.");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendData_WhenConnected()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var tcpClient = new TcpClientService(_config, mockLogger);
        await tcpClient.ConnectAsync();

        var message = "Hello, TCP!";
        await tcpClient.SendMessageAsync(message);

        Assert.True(true); // No exception means success
        await tcpClient.DisconnectAsync();
    }
}
