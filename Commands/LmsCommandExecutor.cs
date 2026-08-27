using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Commands;

public enum LmsStatus
{
    Running,
    Stopped,
    Unknown,
    Error
}

public static class LmsCommandExecutor
{
    private static LmsStatus _cachedStatus = LmsStatus.Unknown;
    private static int _cachedLmStudioPort = 1234;
    private static readonly List<string> _cachedLoadedModels = new();
    private static readonly object _lock = new();

    public static LmsStatus GetStatus()
    {
        lock (_lock)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lms",
                    Arguments = "status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                    process.WaitForExit(10000);

                var output = process?.StandardOutput.ReadToEnd() ?? "";
                var error = process?.StandardError.ReadToEnd() ?? "";

                Logger.Info($"lms status output: [{output}]");
                Logger.Info($"lms status error: [{error}]");

                if (process != null && process.HasExited && process.ExitCode == 0)
                {
                    var text = (output + " " + error).ToLower();
                    if (text.Contains("not running"))
                    {
                        _cachedStatus = LmsStatus.Stopped;
                    }
                    else if (text.Contains("stopped") || text.Contains("stoped"))
                    {
                        _cachedStatus = LmsStatus.Stopped;
                    }
                    else if (text.Contains("running") || text.Contains("started") || text.Contains("port"))
                    {
                        _cachedStatus = LmsStatus.Running;
                    }
                    else
                    {
                        _cachedStatus = LmsStatus.Unknown;
                    }

                    // Парсим порт из вывода
                    var portMatch = Regex.Match(output, @"port[\s:]+(\d+)", RegexOptions.IgnoreCase);
                    if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port) && port > 0)
                    {
                        _cachedLmStudioPort = port;
                        Logger.Info($"Detected LM Studio port from output: {_cachedLmStudioPort}");
                    }

                    // Парсим загруженные модели (объединяем stdout + stderr)
                    // Формат: "  · gpt-oss-20b - 12.11 GB" или "  • gpt-oss-20b - 12.11 GB"
                    _cachedLoadedModels.Clear();
                    var allOutput = output + "\n" + error;
                    var lines = allOutput.Replace("\r", "").Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        // Ищем строки формата "name - size" (например "gpt-oss-20b - 12.11 GB")
                        // Строка должна содержать " - " и заканчиваться на GB/MB
                        if (trimmed.Contains(" - ") && (trimmed.EndsWith("GB") || trimmed.EndsWith("MB") || trimmed.EndsWith("TB")))
                        {
                            // Убираем возможные символы-маркеры в начале (•, ·, -, *)
                            var content = Regex.Replace(trimmed, @"^[••·\-\*]\s*", "").Trim();
                            if (!string.IsNullOrEmpty(content))
                            {
                                _cachedLoadedModels.Add(content);
                            }
                        }
                    }
                    if (_cachedLoadedModels.Count > 0)
                    {
                        Logger.Info($"Loaded models parsed: {string.Join(", ", _cachedLoadedModels)}");
                    }
                }
                else
                {
                    _cachedStatus = LmsStatus.Unknown;
                }

                Logger.Info($"LM Studio status checked: {_cachedStatus}");
                return _cachedStatus;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking LM Studio status: {ex.Message}", ex);
                _cachedStatus = LmsStatus.Error;
                return _cachedStatus;
            }
        }
    }

    public static void StartServer()
    {
        lock (_lock)
        {
            try
            {
                Logger.Info("Starting LM Studio Server...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lms",
                    Arguments = "server start",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit(10000);

                var output = process?.StandardOutput.ReadToEnd() ?? "";
                var error = process?.StandardError.ReadToEnd() ?? "";

                if (process!.HasExited && process!.ExitCode == 0)
                {
                    Logger.Info("LM Studio Server started successfully");
                    _cachedStatus = LmsStatus.Running;
                }
                else
                {
                    Logger.Warning($"LM Studio Server start failed: {error}");
                    _cachedStatus = LmsStatus.Error;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error starting LM Studio Server: {ex.Message}", ex);
                _cachedStatus = LmsStatus.Error;
                throw;
            }
        }
    }

    public static void StopServer()
    {
        lock (_lock)
        {
            try
            {
                Logger.Info("Stopping LM Studio Server...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lms",
                    Arguments = "server stop",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit(10000);

                var output = process?.StandardOutput.ReadToEnd() ?? "";
                var error = process?.StandardError.ReadToEnd() ?? "";

                if (process!.HasExited && process!.ExitCode == 0)
                {
                    Logger.Info("LM Studio Server stopped successfully");
                    _cachedStatus = LmsStatus.Stopped;
                }
                else
                {
                    Logger.Warning($"LM Studio Server stop failed: {error}");
                    _cachedStatus = LmsStatus.Error;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error stopping LM Studio Server: {ex.Message}", ex);
                _cachedStatus = LmsStatus.Error;
                throw;
            }
        }
    }

    public static LmsStatus GetCachedStatus()
    {
        lock (_lock)
            return _cachedStatus;
    }

    public static int GetLmStudioPort()
    {
        lock (_lock)
            return _cachedLmStudioPort;
    }

    public static List<string> GetLoadedModels()
    {
        lock (_lock)
            return new List<string>(_cachedLoadedModels);
    }

    public static void SetLmStudioPort(int port)
    {
        lock (_lock)
        {
            _cachedLmStudioPort = port;
            Logger.Info($"LM Studio port set to: {port}");
        }
    }

    public static bool TryAutoDetectLmStudioPort()
    {
        lock (_lock)
        {
            var status = GetStatus();
            if (status == LmsStatus.Running)
            {
                // GetStatus уже распарсил порт из вывода
                if (_cachedLmStudioPort > 0 && _cachedLmStudioPort != 1234)
                {
                    return true;
                }
                // Если порт не был распарсен, но сервер запущен — пробуем ещё раз
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "lms",
                        Arguments = "server status",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                        process.WaitForExit(10000);

                    var output = process?.StandardOutput.ReadToEnd() ?? "";
                    var portMatch = Regex.Match(output, @"port[\s:]+(\d+)", RegexOptions.IgnoreCase);
                    if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port) && port > 0)
                    {
                        _cachedLmStudioPort = port;
                        Logger.Info($"Auto-detected LM Studio port: {port}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error auto-detecting LM Studio port: {ex.Message}", ex);
                }
            }
            return false;
        }
    }
}
