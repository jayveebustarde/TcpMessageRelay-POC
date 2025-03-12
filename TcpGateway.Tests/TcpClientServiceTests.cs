using System.Net.Sockets;
using System.Text;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using TcpLibrary.Config;
using TcpLibrary.Exceptions;

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
    public async Task ConnectAsync_ShouldThrowTcpConnectionException_WhenConnectionFails()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var failingConfig = new TcpConfig(ServerIp: "192.168.1.200", Port: 9999, MaxRetries: 2);
        var tcpClient = new TcpClientService(failingConfig, mockLogger);

        await Assert.ThrowsAsync<TcpConnectionException>(async () => await tcpClient.ConnectAsync());
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
    public async Task SendMessageAsync_ShouldThrowTcpSendException_WhenNotConnected()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var tcpClient = new TcpClientService(_config, mockLogger);

        await Assert.ThrowsAsync<TcpSendException>(async () => await tcpClient.SendMessageAsync("Hello, TCP!"));
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowTcpSendException_WhenSocketIsDisposed()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var tcpClient = new TcpClientService(_config, mockLogger);
        await tcpClient.ConnectAsync();
        await tcpClient.DisconnectAsync(); // Dispose socket before sending

        await Assert.ThrowsAsync<TcpSendException>(async () => await tcpClient.SendMessageAsync("Hello, TCP!"));
    }

    //[Fact]
    //public async Task MessageReceived_Event_ShouldTrigger_WhenDataIsReceived()
    //{
    //    var mockLogger = new NullLogger<TcpClientService>();
    //    var tcpClient = new TcpClientService(_config, mockLogger);
    //    await tcpClient.ConnectAsync();

    //    bool eventTriggered = false;
    //    byte[] testMessage = Encoding.UTF8.GetBytes("Test Message");

    //    // Subscribe to the MessageReceived event
    //    tcpClient.MessageReceived += (sender, message) =>
    //    {
    //        eventTriggered = true;
    //        Assert.Equal(testMessage, message);
    //    };

    //    // Use reflection to get the private method and manually invoke it
    //    var methodInfo = typeof(TcpClientService).GetMethod("ReceiveMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    //    await (Task)methodInfo.Invoke(tcpClient, [CancellationToken.None]);

    //    Assert.True(eventTriggered, "MessageReceived event should be triggered.");
    //    await tcpClient.DisconnectAsync();
    //}

    [Fact]
    public async Task DisconnectAsync_ShouldTriggerOnDisconnectEvent()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var tcpClient = new TcpClientService(_config, mockLogger);
        await tcpClient.ConnectAsync();

        bool eventTriggered = false;
        tcpClient.OnDisconnect += (sender, reason) => eventTriggered = true;

        await tcpClient.DisconnectAsync();

        Assert.True(eventTriggered, "OnDisconnect event should be triggered.");
    }

    //[Fact]
    //public async Task RelayMessage_ShouldForwardMessage_WhenConnected()
    //{
    //    var mockLogger = new NullLogger<TcpClientService>();
    //    var mockTargetClient = new Mock<IClientService>();
    //    mockTargetClient.Setup(tc => tc.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

    //    var tcpClient = new TcpClientService(_config, mockLogger);
    //    await tcpClient.ConnectAsync();
    //    byte[] testMessage = Encoding.UTF8.GetBytes("Test Relay Message");

    //    tcpClient.MessageReceived += (sender, message) => mockTargetClient.Object.SendMessageAsync(message);
    //    //tcpClient.MessageReceived.Invoke(tcpClient, testMessage);

    //    mockTargetClient.Verify(tc => tc.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    //}

    [Fact]
    public async Task RelayMessage_ShouldNotForward_WhenNotConnected()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var mockTargetClient = new Mock<IClientService>();
        mockTargetClient.Setup(tc => tc.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        var tcpClient = new TcpClientService(_config, mockLogger);
        byte[] testMessage = Encoding.UTF8.GetBytes("Test Relay Message");

        await Assert.ThrowsAsync<TcpSendException>(async () => await tcpClient.SendMessageAsync(testMessage));
        mockTargetClient.Verify(tc => tc.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task ConnectAsync_ShouldRetry_WhenFirstAttemptFails()
    {
        var mockLogger = new NullLogger<TcpClientService>();
        var failingConfig = new TcpConfig(ServerIp: "192.168.1.250", Port: 5050, MaxRetries: 2, RetryDelayMs: 500);
        var tcpClient = new TcpClientService(failingConfig, mockLogger);

        await Assert.ThrowsAsync<TcpConnectionException>(async () => await tcpClient.ConnectAsync());
    }
}