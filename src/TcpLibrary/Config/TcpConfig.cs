namespace TcpLibrary.Config;

/// <summary>
/// The configuration for a TCP client.
/// </summary>
/// <param name="ServerIp">The TCP server ip value.</param>
/// <param name="Port">The TCP server port in use.</param>
/// <param name="MaxRetries">The number of max retries when connecting to the TCP server.</param>
/// <param name="RetryDelayMs">The retry delay when connecting to the TCP server.</param>
public record TcpConfig
(
    string ServerIp,
    int Port,
    int MaxRetries = 5,
    int RetryDelayMs = 3000
);
