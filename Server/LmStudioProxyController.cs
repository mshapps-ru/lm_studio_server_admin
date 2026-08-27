using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        "content-length",
        "transfer-encoding",
    };

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    private static readonly HttpClient _httpClient = new();

    public static void HandleProxyRequest(HttpListenerContext context, AppConfig config)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "/";
        var method = request.HttpMethod;

        // Получаем порт LM Studio
        var lmStudioPort = config.LmStudioPort;
        var targetUrl = $"http://localhost:{lmStudioPort}{path}";

        // Проверяем, является ли путь проксируемым
        var isProxyPath = ProxyPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                          || path.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase);

        if (!isProxyPath)
        {
            SendNotFound(context);
            return;
        }

        // Читаем body заранее
        var bodyBytes = Array.Empty<byte>();
        string requestBodyStr = "";
        if (request.ContentLength64 > 0 && method != "GET" && method != "HEAD")
        {
            var contentLength = (int)request.ContentLength64;
            bodyBytes = new byte[contentLength];
            int bytesRead = 0;
            while (bytesRead < contentLength)
            {
                int read = request.InputStream.Read(bodyBytes, bytesRead, contentLength - bytesRead);
                if (read == 0) break;
                bytesRead += read;
            }
            bodyBytes = bodyBytes.Take(bytesRead).ToArray();
            requestBodyStr = Encoding.UTF8.GetString(bodyBytes);
        }

        // Verbose proxy logging
        if (config.VerboseProxyLogging)
        {
            var headersInfo = string.Join("; ", request.Headers.AllKeys.Select(k => $"{k}: {request.Headers[k]}"));
            Logger.Info($"[PROXY IN] {method} {path}\nHeaders: {headersInfo}\nBody: {requestBodyStr}");
            Logger.Info($"[PROXY OUT] targetUrl: {targetUrl}");
            Logger.Info($"[PROXY OUT] method: {method}");
            Logger.Info($"[PROXY OUT] Body size: {bodyBytes.Length} bytes");
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Создаём HTTP-запрос
            var httpRequest = new HttpRequestMessage(new HttpMethod(method), targetUrl);

            // Копируем заголовки
            foreach (var key in request.Headers.AllKeys)
            {
                if (key != null && !IgnoredHeaders.Contains(key))
                {
                    var value = request.Headers[key];
                    if (value != null)
                    {
                        try
                        {
                            httpRequest.Headers.TryAddWithoutValidation(key, new[] { value });
                        }
                        catch
                        {
                            // Ignore headers that can't be added
                        }
                    }
                }
            }

            // Если в исходном запросе нет заголовка Authorization, но в cookie есть токен – добавим его
            if (!request.Headers.AllKeys.Any(k => string.Equals(k, "Authorization", StringComparison.OrdinalIgnoreCase)))
            {
                var tokenCookie = request.Cookies["token"];
                if (tokenCookie != null)
                {
                    var token = tokenCookie.Value;
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                    }
                }
            }

            // Добавляем body
            if (bodyBytes.Length > 0)
            {
                httpRequest.Content = new ByteArrayContent(bodyBytes);
                httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType ?? "application/json");
            }

            // Отправляем запрос
            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = _httpClient.Send(httpRequest);
            }
            catch (TaskCanceledException) when (config.VerboseProxyLogging)
            {
                var timeoutDuration = DateTime.UtcNow - startTime;
                Logger.Error($"Proxied {method} {path} -> TIMEOUT ({timeoutDuration.TotalMilliseconds} ms): Request timed out");
                throw;
            }

            // Копируем ответ
            var responseStream = httpResponse.Content?.ReadAsStreamAsync().GetAwaiter().GetResult() ?? (Stream)Stream.Null;
            var responseBuffer = new MemoryStream();
            responseStream.CopyTo(responseBuffer);
            responseBuffer.Position = 0;

            context.Response.StatusCode = (int)httpResponse.StatusCode;
            var contentType = httpResponse.Content?.Headers.ContentType?.MediaType;
            context.Response.ContentType = contentType ?? "application/json";

            if (responseBuffer.Length > 0)
            {
                var outBuffer = responseBuffer.ToArray();
                context.Response.ContentLength64 = outBuffer.Length;
                context.Response.OutputStream.Write(outBuffer, 0, outBuffer.Length);
            }

            context.Response.OutputStream.Close();

            var proxyDuration = DateTime.UtcNow - startTime;
            Logger.Info($"Proxied {method} {path} -> {(int)httpResponse.StatusCode} ({proxyDuration.TotalMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.Error($"Proxied {method} {path} -> 502 ({duration.TotalMilliseconds} ms): Proxy error: {ex.Message}", ex);
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
