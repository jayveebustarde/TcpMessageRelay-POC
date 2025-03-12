namespace TcpLibrary.Exceptions;

/// <summary>
/// Base exception class for all TCP client-related errors.
/// </summary>
public class TcpClientException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TcpClientException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public TcpClientException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpClientException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public TcpClientException(string message, Exception innerException) : base(message, innerException) { }
}