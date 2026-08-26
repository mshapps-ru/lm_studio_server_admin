using System.Net;
using System.Text;
using System.Text.Json;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Server;

public static class SettingsController
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
{
    WriteIndented = false,
    PropertyNameCaseInsensitive = true
};

    public static void GetSettings(HttpListenerContext context, AppConfig config)
    {
        var response = JsonSerializer.Serialize(new
        {
            username = config.Username,
            port = config.Port
        }, _jsonOptions);
        SendJsonResponse(context, HttpStatusCode.OK, response);
    }

    public static void UpdateSettings(HttpListenerContext context, AppConfig config)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var body = reader.ReadToEnd();
            Logger.Info("Update settings request body: " + body);
            var newSettings = JsonSerializer.Deserialize<SettingsUpdate>(body, _jsonOptions);
            Logger.Info($"Parsed port: {newSettings?.Port}");

            if (newSettings == null)
            {
                SendJsonResponse(context, HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "Invalid request body" }, _jsonOptions));
                return;
            }

            // Validate port
            int portToSet = newSettings.Port; 
            if (portToSet == 0) portToSet = config.Port; // keep existing if not provided
            if (portToSet < 1 || portToSet > 65535)
            {
                SendJsonResponse(context, HttpStatusCode.BadRequest,
                    JsonSerializer.Serialize(new { error = "Port must be between 1 and 65535" }, _jsonOptions));
                return;
            }

            var oldPort = config.Port;
            var oldUsername = config.Username;

            // Update username if provided
            if (!string.IsNullOrWhiteSpace(newSettings.Username))
                config.Username = newSettings.Username;

            // Update port
            config.Port = portToSet;

            bool passwordChanged = false;
            if (!string.IsNullOrWhiteSpace(newSettings.Password))
            {
                var newHash = ConfigManager.HashPassword(newSettings.Password);
                if (config.Password != newHash)
                {
                    config.Password = newHash;
                    passwordChanged = true;
                }
            }

            // Log changes
            if (oldPort != config.Port)
                Logger.Info($"Port changed from {oldPort} to {config.Port}");
            if (!string.IsNullOrWhiteSpace(newSettings.Username) && oldUsername != config.Username)
                Logger.Info($"Username changed from {oldUsername} to {config.Username}");
            if (passwordChanged)
                Logger.Info("Password changed");

            bool portChanged = oldPort != config.Port;
            ConfigManager.Save(config);
            Logger.Info("Settings updated successfully");

            // Send response BEFORE restarting (server will be stopped)
            SendJsonResponse(context, HttpStatusCode.OK,
                JsonSerializer.Serialize(new { success = true }, _jsonOptions));

            if (portChanged)
            {
                AuthManager.ClearAllSessions(); // invalidate sessions
                Logger.Info("Service restarted due to port change.");
                Program.RestartServices();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error updating settings: {ex.Message}", ex);
            SendJsonResponse(context, HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new { error = "Failed to update settings" }, _jsonOptions));
        }
    }

    private static void SendJsonResponse(HttpListenerContext context, HttpStatusCode statusCode, string json)
    {
        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    private class SettingsUpdate
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int Port { get; set; }
    }
}
