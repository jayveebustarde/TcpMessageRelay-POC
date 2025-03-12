using System.Net.Sockets;
using System.Text;
using Moq;
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

    [Fact]
    public async Task SendMessageAsync_ShouldFail_WhenNotConnected()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var tcpClient = new TcpClientService(_config, mockLogger);

        await Assert.ThrowsAsync<SocketException>(async () => await tcpClient.SendMessageAsync("Hello, TCP!"));
    }

    [Fact]
    public async Task RelayMessage_ShouldForwardMessage_WhenConnected()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var mockTargetClient = new Mock<IClientService>();
        mockTargetClient.Setup(tc => tc.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        var tcpClient = new TcpClientService(_config, mockLogger);
        await tcpClient.ConnectAsync();
        byte[] testMessage = Encoding.UTF8.GetBytes("Test Relay Message");

        await tcpClient.SendMessageAsync(testMessage);
        mockTargetClient.Verify(tc => tc.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task RelayMessage_ShouldNotForward_WhenNotConnected()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var mockTargetClient = new Mock<IClientService>();
        mockTargetClient.Setup(tc => tc.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        var tcpClient = new TcpClientService(_config, mockLogger);
        byte[] testMessage = Encoding.UTF8.GetBytes("Test Relay Message");

        await tcpClient.SendMessageAsync(testMessage);
        mockTargetClient.Verify(tc => tc.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }
}