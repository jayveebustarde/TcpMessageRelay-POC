public interface ITcpMessageRelay
{
    Task<bool> StartRelayAsync();
    Task StopRelayAsync();
}
