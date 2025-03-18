using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using TcpMessageRelay.Clients;
using TcpMessageRelay.Relay;
using TcpLibrary.Exceptions;

namespace TcpMessageRelay.Tests;

public class TcpRelayServiceTests
{
    private readonly Mock<IVehicleClient> _mockVehicleClient;
    private readonly Mock<IBackendClient> _mockBackendClient;
    private readonly Mock<IEventHandler<byte[]>> _mockVehicleToBackendHandler;
    private readonly Mock<IEventHandler<byte[]>> _mockBackendToVehicleHandler;
    private readonly TcpRelayService _relay;

    public TcpRelayServiceTests()
    {
        _mockVehicleClient = new Mock<IVehicleClient>();
        _mockBackendClient = new Mock<IBackendClient>();
        _mockVehicleToBackendHandler = new Mock<IEventHandler<byte[]>>();
        _mockBackendToVehicleHandler = new Mock<IEventHandler<byte[]>>();

        _relay = new TcpRelayService(
            _mockVehicleClient.Object,
            _mockBackendClient.Object,
            _mockVehicleToBackendHandler.Object,
            _mockBackendToVehicleHandler.Object,
            new NullLogger<TcpRelayService>());
    }

    [Fact]
    public async Task StartRelayAsync_ShouldFail_WhenBackendConnectionFails()
    {
        _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
        _mockVehicleClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(false);
        _mockBackendClient.SetupGet(vc => vc.IsConnected).Returns(false);

        bool result = await _relay.StartRelayAsync();

        Assert.False(result, "Relay should fail to start if backend connection fails.");
        _mockVehicleClient.Verify(vc => vc.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task RelayMessage_ShouldNotForward_WhenRelayIsStopped()
    {
        // Arrange
        var encodedMessage = Encoding.UTF8.GetBytes("Test Message");
        await _relay.StartRelayAsync();
        await _relay.StopRelayAsync();

        // Act: Fire the event after stopping the relay
        _mockVehicleClient.Raise(vc => vc.MessageReceived += null, null, encodedMessage);

        // Assert: The handler should not be triggered
        _mockVehicleToBackendHandler.Verify(h => h.HandleAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task RelayMessage_ShouldIgnore_EmptyOrNullMessages()
    {
        // Arrange
        await _relay.StartRelayAsync();
        byte[] emptyMessage = [];

        // Act: Fire event with empty message
        _mockVehicleClient.Raise(vc => vc.MessageReceived += null, null, emptyMessage);

        // Assert: The handler should not process the empty message
        _mockVehicleToBackendHandler.Verify(h => h.HandleAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task RelayMessage_ShouldForwardMessages_FromVehicleToBackend()
    {
        // Arrange
        _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
        _mockVehicleClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);
        _mockBackendClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockVehicleToBackendHandler.Setup(h => h.HandleAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        await _relay.StartRelayAsync();
        var testMessage = Encoding.UTF8.GetBytes("Test Message from Vehicle");

        _mockVehicleClient.Raise(vc => vc.MessageReceived += null!, EventArgs.Empty, testMessage);

        // Assert: Ensure the handler is called
        _mockVehicleToBackendHandler.Verify(h => h.HandleAsync(testMessage), Times.Once);
    }

    [Fact]
    public async Task RelayMessage_ShouldForwardMessages_FromBackendToVehicle()
    {
        // Arrange
        _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
        _mockVehicleClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);
        _mockBackendClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockBackendToVehicleHandler.Setup(h => h.HandleAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        await _relay.StartRelayAsync();
        var testMessage = Encoding.UTF8.GetBytes("Test Message from Backend");

        _mockBackendClient.Raise(bc => bc.MessageReceived += null!, EventArgs.Empty, testMessage);
        await Task.Delay(50);

        // Assert: Ensure the handler is called
        _mockBackendToVehicleHandler.Verify(h => h.HandleAsync(testMessage), Times.Once);
    }

    [Fact]
    public async Task RelayMessage_ShouldHandle_SendFailuresGracefully()
    {
        // Arrange
        _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
        _mockVehicleClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);
        _mockBackendClient.SetupGet(vc => vc.IsConnected).Returns(true);

        _mockVehicleToBackendHandler.Setup(h => h.HandleAsync(It.IsAny<byte[]>()))
            .ThrowsAsync(new TcpSendException("Simulated send failure"));

        await _relay.StartRelayAsync();
        var testMessage = Encoding.UTF8.GetBytes("Message that fails");

        _mockVehicleClient.Raise(vc => vc.MessageReceived += null!, EventArgs.Empty, testMessage);
        await Task.Delay(50);

        // Assert: The exception should be handled, but the handler should still be called
        _mockVehicleToBackendHandler.Verify(h => h.HandleAsync(It.IsAny<byte[]>()), Times.Once);
    }
}
