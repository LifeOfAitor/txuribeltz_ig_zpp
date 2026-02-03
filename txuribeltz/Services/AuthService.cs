using System.IO;
using System.Net.Sockets;
using System.Text;

namespace txuribeltz.Services;

public class AuthService : IAuthService
{
    private readonly string _serverIp;
    private readonly int _serverPort;
    private string? _lastUserType; // ← Store last login response

    public AuthService(string serverIp = "127.0.0.1", int serverPort = 13000)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
    }

    public async Task<bool> ValidateUserAsync(string username, string password)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_serverIp, _serverPort);

            using var ns = client.GetStream();
            using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
            using var reader = new StreamReader(ns, Encoding.UTF8);

            await writer.WriteLineAsync($"LOGIN:{username}:{password}");
            string? response = await reader.ReadLineAsync();

            if (response?.StartsWith("LOGIN_OK") == true)
            {
                // Parse: LOGIN_OK:admin or LOGIN_OK:user
                string[] parts = response.Split(':');
                _lastUserType = parts.Length > 1 ? parts[1] : "user";
                return true;
            }

            _lastUserType = null;
            return false;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Konexio errorea");
        }
    }

    public async Task<bool> IsAdminAsync(string username)
    {
        // ✅ Return cached result from ValidateUserAsync
        return await Task.FromResult(_lastUserType == "admin");
    }
}