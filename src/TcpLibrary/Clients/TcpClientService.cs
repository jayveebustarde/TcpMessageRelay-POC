using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TcpLibrary.Config;
using TcpLibrary.Exceptions;

/// <summary>
/// Initializes a new instance of the <see cref="TcpClientService"/> class.
/// </summary>
/// <param name="config">The TCP configuration.</param>
/// <param name="logger">The logger instance.</param>
public class TcpClientService(TcpConfig config, ILogger<TcpClientService> logger) : IClientService
{
    private readonly TcpConfig _config = config;
    private readonly ILogger<TcpClientService> _logger = logger;
    private Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private CancellationTokenSource? _cts;

    /// <inheritdoc /> 
    public event EventHandler<byte[]>? MessageReceived;

    /// <inheritdoc /> 
    public event EventHandler<string>? OnDisconnect;

    /// <inheritdoc />
    public async Task<bool> ConnectAsync()
    {
        _cts = new CancellationTokenSource();
        for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation($"Attempt {attempt}: Connecting to {_config.ServerIp}:{_config.Port}...");
                await _socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(_config.ServerIp), _config.Port));
                _logger.LogInformation("Connected to server.");
                _ = ReceiveMessagesAsync(_cts.Token);
                return true;
            }
            catch (SocketException ex)
            {
                _logger.LogError($"[SocketException] Connection attempt {attempt} failed: {ex.Message}");
                if (attempt == _config.MaxRetries)
                    throw new TcpConnectionException("Failed to connect after max retries.", ex);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning("[ObjectDisposedException] Connection attempt failed: Socket already disposed.");
                throw new TcpConnectionException("Socket was disposed before connection could be established.", ex);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("[OperationCanceledException] Connection attempt was canceled or timed out.");
                throw new TcpConnectionException("Connection attempt was canceled or exceeded timeout.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Unexpected Exception] Connection failed: {ex.Message}");
                throw new TcpConnectionException("Unexpected error occurred during connection.", ex);
            }
            
            await Task.Delay(_config.RetryDelayMs);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        try
        {
            if (_cts is not null)
                await _cts.CancelAsync();

            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // Recreate socket for reuse
            _logger.LogInformation("Disconnected from server.");

            OnDisconnect?.Invoke(this, "Disconnected from server.");
        }
        catch (SocketException ex)
        {
            _logger.LogError($"[SocketException] Error during disconnect: {ex.Message}");
            throw new TcpClientException("Error while disconnecting.", ex);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogWarning("[ObjectDisposedException] Attempted to disconnect an already disposed socket.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Unexpected Exception] Error during disconnect: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(string message) => 
        await SendMessageAsync(Encoding.UTF8.GetBytes(message));

    /// <inheritdoc />
    public async Task SendMessageAsync(byte[] data)
    {
        if (!_socket.Connected)
        {
            _logger.LogWarning("SendMessage failed: Not connected.");
            throw new TcpSendException("SendMessage failed: Not connected.");
        }

        try
        {
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            byte[] fullMessage = new byte[lengthPrefix.Length + data.Length];
            Buffer.BlockCopy(lengthPrefix, 0, fullMessage, 0, lengthPrefix.Length);
            Buffer.BlockCopy(data, 0, fullMessage, lengthPrefix.Length, data.Length);

            await _socket.SendAsync(new ArraySegment<byte>(fullMessage), SocketFlags.None);
            _logger.LogInformation($"Sent {data.Length} bytes.");
        }
        catch (SocketException ex)
        {
            _logger.LogError($"[SocketException] Failed to send message: {ex.Message}");
            throw new TcpSendException("Error sending message over TCP.", ex);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning("[ObjectDisposedException] Attempted to send a message on a disposed socket.");
            throw new TcpSendException("Socket has been disposed. Unable to send message.", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("[OperationCanceledException] Message sending was canceled or timed out.");
            throw new TcpSendException("Send operation was canceled or exceeded timeout.", ex);
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        byte[] lengthBuffer = new byte[4]; // 4 bytes for message length prefix
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                int lengthBytesRead = await _socket.ReceiveAsync(new ArraySegment<byte>(lengthBuffer), SocketFlags.None);
                if (lengthBytesRead == 0)
                {
                    _logger.LogWarning("Server closed connection. Stopping message reception.");
                    break; // Connection closed by the server
                }

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] messageBuffer = new byte[messageLength];
                int totalBytesRead = 0;

                while (totalBytesRead < messageLength)
                {
                    int bytesRead = await _socket.ReceiveAsync(new ArraySegment<byte>(messageBuffer, totalBytesRead, messageLength - totalBytesRead), SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        _logger.LogWarning("Server disconnected while receiving message.");
                        break;
                    }
                    totalBytesRead += bytesRead;
                }

                MessageReceived?.Invoke(this, messageBuffer);
                _logger.LogInformation($"Received {messageLength} bytes.");
            }
            catch (SocketException ex)
            {
                _logger.LogError($"[SocketException] Receive error: {ex.Message}");
                break; // Exit the loop on connection failure
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("Attempted to read from a disposed socket.");
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Receive operation was canceled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Unexpected Exception] Receive failed: {ex.Message}");
                break;
            }
        }

        _logger.LogWarning("Disconnected from server.");
        await DisconnectAsync();
    }
}
