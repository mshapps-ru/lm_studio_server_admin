using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Server;

public static class AuthManager
{
    private static readonly ConcurrentDictionary<string, (string username, DateTime expires)> _sessions = new();
    private const int SessionTimeoutHours = 24;

    public static (bool success, string message, string? token) Login(string username, string password, AppConfig config)
    {
        var storedHash = IsHashed(config.Password) ? config.Password : HashPassword(config.Password);
        var inputHash = HashPassword(password);

        if (username == config.Username && inputHash == storedHash)
        {
            var token = Guid.NewGuid().ToString("N");
            _sessions[token] = (username, DateTime.UtcNow.AddHours(SessionTimeoutHours));

            // Если пароль был plain text — обновляем конфиг хешем
            if (!IsHashed(config.Password))
            {
                config.Password = storedHash;
                Config.ConfigManager.Save(config);
                Logger.Info($"Password hashed and saved for user '{username}'");
            }

            Logger.Info($"User '{username}' logged in");
            return (true, "Logged in", token);
        }

        Logger.Warning($"Failed login attempt for user '{username}'");
        return (false, "Invalid username or password", null);
    }

    public static void Logout(string token)
    {
        if (_sessions.TryRemove(token, out _))
        {
            Logger.Info("Session ended");
        }
    }

    public static bool TryValidateToken(string token, out string username)
    {
        username = string.Empty;

        if (string.IsNullOrEmpty(token))
            return false;

        if (_sessions.TryGetValue(token, out var session))
        {
            if (DateTime.UtcNow < session.expires)
            {
                username = session.username;
                return true;
            }
            else
            {
                // Session expired
                _sessions.TryRemove(token, out _);
                Logger.Info($"Expired session removed for user '{session.username}'");
            }
        }

        return false;
    }

    public static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool IsHashed(string value)
    {
        // SHA-256 Base64 hash всегда 44 символа и содержит только допустимые Base64 символы
        return value.Length == 44 && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9+/]+=*$");
    }
}
