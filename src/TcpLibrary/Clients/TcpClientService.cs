using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TcpLibrary.Config;

/// <summary>
/// TCP Client Service for connecting to a TCP server.
/// </summary>
/// <param name="config">The TCP configuration.</param>
/// <param name="logger">The logger instance.</param>
public class TcpClientService(TcpConfig config, ILogger<TcpClientService> logger) : IClientService
{
    private readonly TcpConfig _config = config;
    private readonly ILogger<TcpClientService> _logger = logger;
    private Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private CancellationTokenSource? _cts;

    public event EventHandler<byte[]>? MessageReceived;
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
            catch (Exception ex)
            {
                _logger.LogError($"Connection attempt {attempt} failed: {ex.Message}");
                if (attempt < _config.MaxRetries) await Task.Delay(_config.RetryDelayMs);
                else return false;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if(_cts is not null)
            await _cts.CancelAsync();

        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();

        if (OnDisconnect is not null)
            OnDisconnect?.Invoke(this, "Disconnected from server.");

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // Recreate socket for reuse
        _logger.LogInformation("Disconnected from server.");
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(string message) => 
        await SendMessageAsync(Encoding.UTF8.GetBytes(message));

    public async Task SendMessageAsync(byte[] data)
    {
        if (!_socket.Connected)
        {
            _logger.LogWarning("SendMessage failed: Not connected.");
            return;
        }

        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        byte[] fullMessage = new byte[lengthPrefix.Length + data.Length];
        Buffer.BlockCopy(lengthPrefix, 0, fullMessage, 0, lengthPrefix.Length);
        Buffer.BlockCopy(data, 0, fullMessage, lengthPrefix.Length, data.Length);

        await _socket.SendAsync(new ArraySegment<byte>(fullMessage), SocketFlags.None);
        _logger.LogInformation($"Sent {data.Length} bytes.");
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        byte[] lengthBuffer = new byte[4]; // 4 bytes for message length prefix
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                int lengthBytesRead = await _socket.ReceiveAsync(new ArraySegment<byte>(lengthBuffer), SocketFlags.None);
                if (lengthBytesRead == 0) break;

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] messageBuffer = new byte[messageLength];
                int totalBytesRead = 0;

                while (totalBytesRead < messageLength)
                {
                    int bytesRead = await _socket.ReceiveAsync(new ArraySegment<byte>(messageBuffer, totalBytesRead, messageLength - totalBytesRead), SocketFlags.None);
                    if (bytesRead == 0) break;
                    totalBytesRead += bytesRead;
                }

                // Fire event with raw bytes instead of string
                MessageReceived?.Invoke(this, messageBuffer);
                _logger.LogInformation($"Received {messageLength} bytes.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Receive error: {ex.Message}");
                break;
            }
        }
        _logger.LogWarning("Disconnected from server.");
        await DisconnectAsync();
    }

}
