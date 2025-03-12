namespace TcpLibrary.Exceptions;

/// <summary>
/// Exception thrown when receiving a message over TCP fails.
/// </summary>
public class TcpReceiveException : TcpClientException
{
    /// <inheritdoc />
    public TcpReceiveException(string message) : base(message) { }

    /// <inheritdoc />
    public TcpReceiveException(string message, Exception innerException) : base(message, innerException) { }
}