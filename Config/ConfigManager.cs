using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Config;

public static class ConfigManager
{
    private static readonly string _configFilePath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    static ConfigManager()
    {
        // Determine the directory of the executing assembly (exe/dll)
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(exeDir))
            _configFilePath = Path.Combine(exeDir, "config.json");
        else
            // Fallback to AppDomain base directory if reflection fails
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    }

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                Logger.Info("config.json not found, creating with defaults");
                var defaultConfig = new AppConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
            if (config == null)
            {
                Logger.Warning("Deserialized config is null, using defaults");
                config = new AppConfig();
                Save(config);
            }
            return config;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading config: {ex.Message}", ex);
            Logger.Info("Creating config with defaults");
            var defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }
    }

    public static void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configFilePath, json, Encoding.UTF8);
            Logger.Info("Config saved successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving config: {ex.Message}", ex);
            throw;
        }
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
