using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using TcpMessageRelay.Clients;
using TcpMessageRelay.Relay;

namespace TcpGateway.Tests
{
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
        public async Task StartRelayAsync_ShouldNotStartBackend_WhenVehicleConnectionFails()
        {
            _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(false);
            _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);

            bool result = await _relay.StartRelayAsync();

            Assert.False(result, "Backend should NOT connect if vehicle connection fails.");
            _mockBackendClient.Verify(bc => bc.ConnectAsync(), Times.Never);
        }

        [Fact]
        public async Task StartRelayAsync_ShouldStartBackend_WhenVehicleConnectsSuccessfully()
        {
            _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
            _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);

            bool result = await _relay.StartRelayAsync();

            Assert.True(result, "Backend should connect if vehicle connects.");
            _mockBackendClient.Verify(bc => bc.ConnectAsync(), Times.Once);
        }

        [Fact]
        public async Task RelayMessage_ShouldForwardMessages_FromVehicleToBackend()
        {
            // Arrange
            _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
            _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);
            _mockVehicleClient.Setup(bc => bc.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            await _relay.StartRelayAsync(); // Ensure relay is running
            var encodedMessage = Encoding.UTF8.GetBytes("Test Message from Backend");

            // Act: Fire the event to simulate a message from the vehicle
            _mockVehicleClient.Raise(vc => vc.MessageReceived += null, null, encodedMessage);

            // Assert
            _mockBackendClient.Verify(bc => bc.SendMessageAsync(encodedMessage), Times.Once);
        }


        [Fact]
        public async Task RelayMessage_ShouldForwardMessages_FromBackendToVehicle()
        {
            // Arrange
            _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
            _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);
            _mockBackendClient.Setup(bc => bc.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            await _relay.StartRelayAsync(); // Ensure relay is running
            var encodedMessage = Encoding.UTF8.GetBytes("Test Message from Backend");

            // Act: Fire the event to simulate a message from the vehicle
            _mockBackendClient.Raise(bc => bc.MessageReceived += null, null, encodedMessage);

            // Assert
            _mockVehicleClient.Verify(vc => vc.SendMessageAsync(encodedMessage), Times.Once);
        }
    }

}
