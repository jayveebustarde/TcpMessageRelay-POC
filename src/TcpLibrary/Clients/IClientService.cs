namespace TcpLibrary.Clients;

/// <summary>
/// Interface for a client connection service that is not specific to any protocol.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Event raised when a message is received from the server.
    /// </summary>
    event EventHandler<byte[]> MessageReceived;

    /// <summary>
    /// Event raised when the client is disconnected from the server.
    /// </summary>
    event EventHandler<string> OnDisconnect;

    /// <summary>
    /// Connect to the server.
    /// </summary>
    /// <returns></returns>
    Task<bool> ConnectAsync();

    /// <summary>
    /// Disconnect from the server.
    /// </summary>
    /// <returns>Asynchronous task of the action</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Send a message to the server.
    /// </summary>
    /// <param name="message">The string message input.</param>
    /// <returns>Asynchronous task of the action</returns>
    Task SendMessageAsync(string message);

    /// <summary>
    /// Send a message to the server.
    /// </summary>
    /// <param name="data">The byte array data input.</param>
    /// <returns>Asynchronous task of the action</returns>
    Task SendMessageAsync(byte[] data);

    /// <summary>
    /// Gets a value indicating whether the client is connected to the server.
    /// </summary>
    bool IsConnected { get; }
}
