public class TcpConfig
{
    public string ServerIp { get; set; }
    public int Port { get; set; }
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayMs { get; set; } = 3000;
}
