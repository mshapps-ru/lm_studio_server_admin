using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text; // for StringBuilder
using LmStudioServerAdmin.Config;
using System.Linq;
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
                var config = ConfigManager.Load();
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

                if (config.VerboseLogging)
                {
                    Logger.Info($"lms status output: [{output}]");
                    Logger.Info($"lms status error: [{error}]");
                }

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
                    var seenBase = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.Contains(" - ") && (trimmed.EndsWith("GB") || trimmed.EndsWith("MB") || trimmed.EndsWith("TB")))
                        {
                            var content = Regex.Replace(trimmed, @"^[••·\-\*]\s*", "").Trim();
                            if (!string.IsNullOrEmpty(content))
                            {
                                var baseName = GetBaseName(content);
                                if (seenBase.Add(baseName)) // add only first occurrence
                                    _cachedLoadedModels.Add(content);
                            }
                        }
                    }
                    if (config.VerboseLogging && _cachedLoadedModels.Count > 0)
                    {
                        Logger.Info($"Loaded models parsed: {string.Join(", ", _cachedLoadedModels)}");
                    }
                }
                else
                {
                    _cachedStatus = LmsStatus.Unknown;
                }

                if (config.VerboseLogging)
                {
                    Logger.Info($"LM Studio status checked: {_cachedStatus}");
                }

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

    // Load a model via `lms load <name>` after unloading all previous models.
    public static bool LoadModel(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return false;

        // Load configuration for default and override parameters
        var config = ConfigManager.Load();
        var finalParams = new Dictionary<string, int?>();
        if (config.LmStudioModelDefaultLoadParameter != null)
            foreach (var kv in config.LmStudioModelDefaultLoadParameter)
                finalParams[kv.Key] = kv.Value;

        var overrideEntry = config.LmStudioModelLoadParameterList?.FirstOrDefault(e => string.Equals(e.Model, modelName, StringComparison.OrdinalIgnoreCase));
        if (overrideEntry?.Parameters != null)
            foreach (var kv in overrideEntry.Parameters)
                finalParams[kv.Key] = kv.Value;

        lock (_lock)
        {
            try
            {
                UnloadAllModels();

                var baseModel = GetBaseName(modelName);
                var argsBuilder = new StringBuilder($"load {baseModel}");
                AppendParams(argsBuilder, finalParams);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "lms",
                    Arguments = argsBuilder.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process == null) return false;
                process.WaitForExit(10000);
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                if (!process.HasExited || process.ExitCode != 0)
                {
                    Logger.Error($"Failed to load model {modelName}: {error}");
                    return false;
                }
                _cachedLoadedModels.Add(baseModel);
                Logger.Info($"Model {baseModel} loaded successfully");

                var start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalSeconds < 5)
                {
                    var loaded = GetLoadedModels();
                    if (loaded.Any(m => GetBaseName(m).Equals(baseModel, StringComparison.OrdinalIgnoreCase)))
                        break;
                    System.Threading.Thread.Sleep(500);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading model {modelName}: {ex.Message}", ex);
                return false;
            }
        }
    }

    // Helper to get base name (part before ':' if present)
    private static string GetBaseName(string fullName) => fullName.Contains(":") ? fullName.Split(':')[0] : fullName;

    // Helper to append parameters as CLI flags
    private static void AppendParams(StringBuilder builder, Dictionary<string,int?> parameters)
    {
        foreach(var kv in parameters)
        {
            if(!kv.Value.HasValue) continue;
            string flag = kv.Key switch
            {
                "contextWindow" => $"--context-length {kv.Value.Value}",
                _ => $"--{kv.Key} {kv.Value.Value}",
            };
            builder.Append(' ' + flag);
        }
    }

    // Helper to unload a specific model instance.
    private static bool UnloadModel(string modelName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "lms",
            Arguments = $"unload {modelName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        if (process == null) return false;
        process.WaitForExit(10000);
        var error = process.StandardError.ReadToEnd();
        if (!process.HasExited || process.ExitCode != 0)
        {
            Logger.Error($"Failed to unload model {modelName}: {error}");
            return false;
        }
        _cachedLoadedModels.RemoveAll(m => m.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        Logger.Info($"Model {modelName} unloaded successfully");
        return true;
    }

    // Unload all models using lms unload --all.
    private static void UnloadAllModels()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "lms",
            Arguments = "unload --all",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        if (process == null) return;
        process.WaitForExit(10000);
        _cachedLoadedModels.Clear();
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
