using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LmStudioServerAdmin.Commands;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Server;

public static class HomeController
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    private static string? _lastModelDisplay;

    public static void GetStatus(HttpListenerContext context)
    {
        // Get current status directly (updates cache)
        var status = LmsCommandExecutor.GetStatus();
        var message = status switch
        {
            LmsStatus.Running => "LM Studio Server is running",
            LmsStatus.Stopped => "LM Studio Server is stopped",
            LmsStatus.Unknown => "Unable to determine status",
            LmsStatus.Error => "Error checking status",
            _ => "Unknown status"
        };

        var response = JsonSerializer.Serialize(new { status = status.ToString().ToLower(), message }, _jsonOptions);
        SendJsonResponse(context, HttpStatusCode.OK, response);
    }

    public static void StartServer(HttpListenerContext context)
    {
        try
        {
            LmsCommandExecutor.StartServer();
            var response = JsonSerializer.Serialize(new { success = true }, _jsonOptions);
            SendJsonResponse(context, HttpStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error starting server: {ex.Message}", ex);
            SendJsonResponse(context, HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new { error = "Failed to start server" }, _jsonOptions));
        }
    }

    public static void StopServer(HttpListenerContext context)
    {
        try
        {
            LmsCommandExecutor.StopServer();
            var response = JsonSerializer.Serialize(new { success = true }, _jsonOptions);
            SendJsonResponse(context, HttpStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error stopping server: {ex.Message}", ex);
            SendJsonResponse(context, HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new { error = "Failed to stop server" }, _jsonOptions));
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

    public static void GetLmStudioInfo(HttpListenerContext context, Config.AppConfig config)
    {
        // Обновляем статус и модели из lms status
        var currentStatus = LmsCommandExecutor.GetStatus();
        var lmStudioPort = LmsCommandExecutor.GetLmStudioPort();
        var loadedModels = LmsCommandExecutor.GetLoadedModels();

        string modelDisplay;
        if (loadedModels.Count > 0)
        {
            modelDisplay = string.Join(", ", loadedModels);
        }
        else
        {
            modelDisplay = "No model loaded";
        }

        // Логирование только при изменении модели
        if (config.VerboseProxyLogging && _lastModelDisplay != modelDisplay)
        {
            Logger.Info($"Model changed: {_lastModelDisplay ?? "null"} -> {modelDisplay}");
            _lastModelDisplay = modelDisplay;
        }

        var response = JsonSerializer.Serialize(new
        {
            port = lmStudioPort,
            status = currentStatus.ToString().ToLower(),
            model = modelDisplay,
            connected = currentStatus == LmsStatus.Running
        }, _jsonOptions);

        SendJsonResponse(context, HttpStatusCode.OK, response);
    }

    private class ModelsResponse
    {
        [JsonPropertyName("data")]
        public List<ModelInfo> Data { get; set; } = new();
    }

    private class ModelInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }
}
