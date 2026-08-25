using System.Diagnostics;
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
                    Arguments = "server status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit(5000);

                var output = process?.StandardOutput.ReadToEnd() ?? "";
                var error = process?.StandardError.ReadToEnd() ?? "";

                if (process!.HasExited && process!.ExitCode == 0)
                {
                    if (output.Contains("running", StringComparison.OrdinalIgnoreCase) ||
                        output.Contains("Started", StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedStatus = LmsStatus.Running;
                    }
                    else if (output.Contains("stopped", StringComparison.OrdinalIgnoreCase) ||
                             output.Contains("Stopped", StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedStatus = LmsStatus.Stopped;
                    }
                    else
                    {
                        _cachedStatus = LmsStatus.Unknown;
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
}
