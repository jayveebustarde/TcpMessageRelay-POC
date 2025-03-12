using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TcpGateway.Tests
{
    public class TcpMessageRelayTests
    {
        private readonly Mock<IClientService> _mockVehicleClient;
        private readonly Mock<IClientService> _mockBackendClient;
        private readonly TcpMessageRelay _relay;

        public TcpMessageRelayTests()
        {
            _mockVehicleClient = new Mock<IClientService>();
            _mockBackendClient = new Mock<IClientService>();

            _relay = new TcpMessageRelay(_mockVehicleClient.Object, _mockBackendClient.Object, new NullLogger<TcpMessageRelay>());
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
            _mockVehicleClient.Setup(bc => bc.SendMessageAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var relay = new TcpMessageRelay(_mockVehicleClient.Object, _mockBackendClient.Object, new NullLogger<TcpMessageRelay>());

            await relay.StartRelayAsync(); // Ensure relay is running

            // Act: Fire the event to simulate a message from the vehicle
            _mockVehicleClient.Raise(vc => vc.MessageReceived += null, null, "Test Message from Vehicle");

            // Assert
            _mockBackendClient.Verify(bc => bc.SendMessageAsync("Test Message from Vehicle"), Times.Once);
        }


        [Fact]
        public async Task RelayMessage_ShouldForwardMessages_FromBackendToVehicle()
        {
            // Arrange
            _mockVehicleClient.Setup(vc => vc.ConnectAsync()).ReturnsAsync(true);
            _mockBackendClient.Setup(bc => bc.ConnectAsync()).ReturnsAsync(true);
            _mockBackendClient.Setup(bc => bc.SendMessageAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var relay = new TcpMessageRelay(_mockVehicleClient.Object, _mockBackendClient.Object, new NullLogger<TcpMessageRelay>());

            await relay.StartRelayAsync(); // Ensure relay is running

            // Act: Fire the event to simulate a message from the vehicle
            _mockBackendClient.Raise(bc => bc.MessageReceived += null, null, "Test Message from Backend");

            // Assert
            _mockVehicleClient.Verify(vc => vc.SendMessageAsync("Test Message from Backend"), Times.Once);
        }
    }

}
