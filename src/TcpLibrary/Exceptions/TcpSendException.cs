namespace TcpLibrary.Exceptions;

/// <summary>
/// Exception thrown when sending a message over TCP fails.
/// </summary>
public class TcpSendException : TcpClientException
{
    /// <inheritdoc />
    public TcpSendException(string message) : base(message) { }

    /// <inheritdoc />
    public TcpSendException(string message, Exception innerException) : base(message, innerException) { }
}