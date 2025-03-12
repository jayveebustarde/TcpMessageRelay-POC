namespace TcpLibrary.Exceptions;

/// <summary>
/// Exception thrown when a TCP connection attempt fails.
/// </summary>
public class TcpConnectionException : TcpClientException
{
    /// <inheritdoc />
    public TcpConnectionException(string message) : base(message) { }

    /// <inheritdoc />
    public TcpConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
