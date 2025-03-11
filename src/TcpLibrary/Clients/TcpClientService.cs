using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class TcpClientService(TcpConfig config, ILogger<TcpClientService> logger) : ITcpClientService
{
    private readonly TcpConfig _config = config;
    private readonly ILogger<TcpClientService> _logger = logger;
    private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private CancellationTokenSource _cts;

    public event EventHandler<string> MessageReceived;
    public event EventHandler<string> OnDisconnect;

    public async Task<bool> ConnectAsync()
    {
        _cts = new CancellationTokenSource();
        for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation($"Attempt {attempt}: Connecting to {_config.ServerIp}:{_config.Port}...");
                await _socket.ConnectAsync(new IPEndPoint(System.Net.IPAddress.Parse(_config.ServerIp), _config.Port));
                _logger.LogInformation("✅ Connected to server.");
                _ = ReceiveMessagesAsync(_cts.Token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Connection attempt {attempt} failed: {ex.Message}");
                if (attempt < _config.MaxRetries) await Task.Delay(_config.RetryDelayMs);
                else return false;
            }
        }
        return false;
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // Recreate socket for reuse
        _logger.LogInformation("🔌 Disconnected from server.");
    }

    public async Task SendMessageAsync(string message)
    {
        if (_socket.Connected)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await _socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            _logger.LogInformation($"📤 Sent: {message}");
        }
        else
        {
            _logger.LogWarning("⚠️ SendMessage failed: Not connected.");
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024];
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                // 🔥 Raise Event for Subscribers
                MessageReceived?.Invoke(this, message);

                _logger.LogInformation($"📩 Received: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"⚠️ Receive error: {ex.Message}");
                break;
            }
        }
        _logger.LogWarning("🚫 Disconnected from server.");
        await DisconnectAsync();
    }
}
