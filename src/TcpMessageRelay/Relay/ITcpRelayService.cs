namespace TcpMessageRelay.Relay; 

public interface ITcpRelayService
{
    Task<bool> StartRelayAsync();
    Task StopRelayAsync();
}
