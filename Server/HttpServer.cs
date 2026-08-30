using System.Net;
using System.Text;
using LmStudioServerAdmin.Commands;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Logging;
using System.Text.RegularExpressions;

namespace LmStudioServerAdmin.Server;

public class HttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _wwwRoot;
    private readonly AppConfig _config;
    private bool _running;
    private Thread? _thread;

    public HttpServer(AppConfig config)
    {
        _config = config;
        _listener = new HttpListener();

        // HttpListener на Windows не поддерживает 0.0.0.0 — используем + для всех IPv4
        var prefix = $"http://{(config.BindAddress == "0.0.0.0" ? "+" : config.BindAddress)}:{config.Port}/";
        _listener.Prefixes.Add(prefix);
        Logger.Info($"HttpServer configured on {config.BindAddress}:{config.Port}");
        // Determine wwwroot relative to executable location
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        _wwwRoot = Path.GetFullPath(Path.Combine(exeDir, "wwwroot"));
        Logger.Info($"Resolved wwwroot: {_wwwRoot}");
    }

    public void Start()
    {
        if (_running)
            return;

        try
        {
            _listener.Start();
            _running = true;
            _thread = new Thread(ListenLoop) { IsBackground = true };
            _thread.Start();
            Logger.Info($"HttpServer started on port {_config.Port}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start HttpServer: {ex.Message}", ex);
            throw;
        }
    }

    public void Stop()
    {
        _running = false;
        try
        {
            _listener.Stop();
            Logger.Info("HttpServer stopped");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error stopping HttpServer: {ex.Message}", ex);
        }
    }

    private void ListenLoop()
    {
        while (_running && _listener.IsListening)
        {
            try
            {
                var context = _listener.GetContext();
                Task.Run(() => HandleRequest(context));
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_running)
                    Logger.Error($"Error accepting connection: {ex.Message}", ex);
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var path = request.Url?.AbsolutePath ?? "/";
            var method = request.HttpMethod;

            Logger.Info($"{method} {path}");

            // Авторизация — проверяем Authorization header
            var token = ExtractToken(context);
            bool isAuthenticated = AuthManager.TryValidateToken(token ?? string.Empty, out _);

            // Также проверяем cookie как fallback
            if (!isAuthenticated)
            {
                var cookieToken = ExtractTokenFromCookie(context);
                isAuthenticated = AuthManager.TryValidateToken(cookieToken ?? string.Empty, out _);
                if (isAuthenticated)
                    token = cookieToken;
            }

            // Login endpoint
            if (path == "/api/login" && method == "POST")
            {
                HandleLogin(context);
                return;
            }

            // Logout endpoint
            if (path == "/api/logout" && method == "POST")
            {
                HandleLogout(context, token);
                return;
            }

            // API endpoints (require auth)
            if (path.StartsWith("/api/") && !isAuthenticated)
            {
                SendJsonResponse(context, HttpStatusCode.Unauthorized,
                    "{\"error\":\"Unauthorized\"}");
                return;
            }

            // Home API
            if (path == "/api/status" && method == "GET")
            {
                HomeController.GetStatus(context);
                return;
            }

            if (path == "/api/start" && method == "POST")
            {
                HomeController.StartServer(context);
                return;
            }

            if (path == "/api/stop" && method == "POST")
            {
                HomeController.StopServer(context);
                return;
            }

            // LM Studio Info
if (path == "/api/lmstudio/info" && method == "GET")
{
    HomeController.GetLmStudioInfo(context, _config);
    return;
}
// Models API
            if (path == "/api/models" && method == "GET")
            {
                ModelsController.GetModels(context, _config);
                return;
            }
            else if (path == "/api/models/default" && method == "POST")
            {
                ModelsController.PostDefaults(context, _config);
                return;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(path, @"^/api/models/(?<model>[^/]+)/override$") && method == "PUT")
            {
                var match = System.Text.RegularExpressions.Regex.Match(path, @"^/api/models/(?<model>[^/]+)/override$");
                ModelsController.PutOverride(context, match.Groups["model"].Value, _config);
                return;
            }


            // Settings API
            if (path == "/api/settings" && method == "GET")
            {
                SettingsController.GetSettings(context, _config);
                return;
            }

            if (path == "/api/settings" && method == "PUT")
            {
                SettingsController.UpdateSettings(context, _config);
                return;
            }

            // LM Studio Settings API
            if (path == "/api/settings/lmstudio" && method == "GET")
            {
                SettingsController.GetLmStudioSettings(context, _config);
                return;
            }

            if (path == "/api/settings/lmstudio" && method == "PUT")
            {
                SettingsController.UpdateLmStudioSettings(context, _config);
                return;
            }

            if (path == "/api/settings/lmstudio/detect" && method == "POST")
            {
                SettingsController.AutoDetectLmStudioPort(context);
                return;
            }

            // LM Studio Proxy
            if (path.StartsWith("/api/v1/") || path.StartsWith("/v1/"))
            {
                LmStudioProxyController.HandleProxyRequest(context, _config);
                return;
            }

            // Static files
            if (path == "/" || path == "/index.html")
            {
                var indexPath = Path.Combine(_wwwRoot, "index.html");
                Logger.Info($"Serving index from: {indexPath}");
                ServeFile(context, "text/html", indexPath);
                return;
            }

            if (path.StartsWith("/css/"))
            {
                var cssPath = Path.Combine(_wwwRoot, "css", path.Substring(5));
                if (File.Exists(cssPath))
                    ServeFile(context, "text/css", cssPath);
                else
                    SendNotFound(context);
                return;
            }

            if (path.StartsWith("/js/"))
            {
                var jsPath = Path.Combine(_wwwRoot, "js", path.Substring(4));
                if (File.Exists(jsPath))
                    ServeFile(context, "application/javascript", jsPath);
                else
                    SendNotFound(context);
                return;
            }

            // Unknown route
            SendNotFound(context);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling request: {ex.Message}", ex);
            try
            {
                SendJsonResponse(context, HttpStatusCode.InternalServerError,
                    "{\"error\":\"Internal server error\"}");
            }
            catch { }
        }
    }

    private void HandleLogin(HttpListenerContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var body = reader.ReadToEnd();
            var loginData = System.Text.Json.JsonSerializer.Deserialize<LoginData>(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (loginData == null || string.IsNullOrWhiteSpace(loginData.Username) ||
                string.IsNullOrWhiteSpace(loginData.Password))
            {
                SendJsonResponse(context, HttpStatusCode.BadRequest,
                    "{\"error\":\"Username and password are required\"}");
                return;
            }

            var (success, message, token) = AuthManager.Login(loginData.Username, loginData.Password, _config);

            if (success && token != null)
            {
                // Отправляем токен в ответе — клиент сохраняет его в localStorage
                SendJsonResponse(context, HttpStatusCode.OK,
                    System.Text.Json.JsonSerializer.Serialize(new { token, message }));
            }
            else
            {
                SendJsonResponse(context, HttpStatusCode.Unauthorized,
                    System.Text.Json.JsonSerializer.Serialize(new { error = message }));
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during login: {ex.Message}", ex);
            SendJsonResponse(context, HttpStatusCode.InternalServerError,
                "{\"error\":\"Login failed\"}");
        }
    }

    private void HandleLogout(HttpListenerContext context, string? token)
    {
        if (!string.IsNullOrEmpty(token))
            AuthManager.Logout(token);

        SendJsonResponse(context, HttpStatusCode.OK,
            "{\"success\":true}");
    }

    private string? ExtractToken(HttpListenerContext context)
    {
        // Сначала проверяем Authorization header (Bearer token)
        var authHeader = context.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring(7);
        }
        // Fallback — cookie
        return ExtractTokenFromCookie(context);
    }

    private string? ExtractTokenFromCookie(HttpListenerContext context)
    {
        var cookie = context.Request.Cookies["token"];
        return cookie?.Value;
    }

    private void ServeFile(HttpListenerContext context, string contentType, string filePath)
    {
        try
        {
            var fullpath = Path.GetFullPath(filePath);
            Logger.Info($"Attempting to serve file: {fullpath}");
            if (!File.Exists(fullpath))
            {
                Logger.Warning("File not found: " + fullpath);
                SendNotFound(context);
                return;
            }

            var buffer = File.ReadAllBytes(fullpath);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error serving file {filePath}: {ex.Message}", ex);
            SendNotFound(context);
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

    public void Dispose()
    {
        Stop();
    }

    private class LoginData
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
