using System.Net;
using System.Text;
using System.Text.Json;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Server;

public static class LmStudioProxyController
{
    private static readonly string[] ProxyPaths = new[]
    {
        "/api/v1/models",
        "/api/v1/chat",
        "/api/v1/models/load",
        "/api/v1/models/download",
        "/v1/models",
        "/v1/responses",
        "/v1/chat/completions",
        "/v1/completions",
        "/v1/embeddings"
    };

    private static readonly HashSet<string> IgnoredHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "host",
        "connection",
        "content-length"
    };

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public static void HandleProxyRequest(HttpListenerContext context, AppConfig config)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "/";
        var method = request.HttpMethod;

        // Проверяем, является ли путь проксируемым
        var isProxyPath = ProxyPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                          || path.StartsWith("/api/v1/models/download/status", StringComparison.OrdinalIgnoreCase);

        if (!isProxyPath)
        {
            SendNotFound(context);
            return;
        }

        // Получаем порт LM Studio
        var lmStudioPort = config.LmStudioPort;
        var targetUrl = $"http://localhost:{lmStudioPort}{path}";

        // Создаём прокси-запрос
        try
        {
            var proxyRequest = (HttpWebRequest)WebRequest.Create(targetUrl);
            proxyRequest.Method = method;
            proxyRequest.ContentType = request.ContentType ?? "application/json";
            proxyRequest.Timeout = 300000; // 5 минут для долгих запросов

            // Копируем заголовки (исключая некоторые)
            foreach (var key in request.Headers.AllKeys)
            {
                if (key != null && !IgnoredHeaders.Contains(key))
                {
                    try { proxyRequest.Headers.Add(key, request.Headers[key]!); } catch { }
                }
            }

            // Копируем тело запроса
            if (request.ContentLength64 > 0 && method != "GET" && method != "HEAD")
            {
                using var inputStream = request.InputStream;
                using var ms = new MemoryStream();
                inputStream.CopyTo(ms);
                proxyRequest.ContentLength = ms.Length;
                if (ms.Length > 0)
                {
                    using var requestStream = proxyRequest.GetRequestStream();
                    ms.Position = 0;
                    ms.CopyTo(requestStream);
                }
            }

            // Получаем ответ
            using var response = (HttpWebResponse)proxyRequest.GetResponse();
            var responseStream = response.GetResponseStream();

            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.ContentType ?? "application/json";

            if (responseStream != null)
            {
                responseStream.CopyTo(context.Response.OutputStream);
            }

            context.Response.OutputStream.Close();
        }
        catch (WebException ex)
        {
            // LM Studio недоступен
            var errorStatus = ((ex.Response as HttpWebResponse)?.StatusCode ?? HttpStatusCode.BadGateway);
            SendJsonResponse(context, errorStatus,
                JsonSerializer.Serialize(new { error = $"LM Studio Server unavailable (port {lmStudioPort})" }, _jsonOptions));
        }
        catch (Exception ex)
        {
            Logger.Error($"Proxy error: {ex.Message}", ex);
            SendJsonResponse(context, HttpStatusCode.BadGateway,
                JsonSerializer.Serialize(new { error = "Proxy error" }, _jsonOptions));
        }
    }

    private static void SendNotFound(HttpListenerContext context)
    {
        var msg = Encoding.UTF8.GetBytes("404 Not Found");
        context.Response.StatusCode = 404;
        context.Response.ContentLength64 = msg.Length;
        context.Response.OutputStream.Write(msg, 0, msg.Length);
        context.Response.OutputStream.Close();
    }

    private static void SendJsonResponse(HttpListenerContext context, HttpStatusCode statusCode, string json)
    {
        try
        {
            var buffer = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending JSON response: {ex.Message}", ex);
        }
    }
}
