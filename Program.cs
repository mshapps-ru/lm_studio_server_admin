using LmStudioServerAdmin.Commands;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Logging;
using LmStudioServerAdmin.Server;
using LmStudioServerAdmin.Service;

namespace LmStudioServerAdmin;

public static class Program
{
    private static HttpServer? _httpServer;
    private static StatusChecker? _statusChecker;
    private static ModelListUpdater? _modelListUpdater;
    private static AppConfig? _config;

    public static void Main(string[] args)
    {
        var isService = args.Contains("-service");

        Logger.Info("=== LmStudioServerAdmin starting ===");

        // Загрузка настроек
        _config = ConfigManager.Load();
        LmsCommandExecutor.SetLmStudioPort(_config.LmStudioPort);
        Logger.Info($"Config loaded: username={_config.Username}, port={_config.Port}, lmStudioPort={_config.LmStudioPort}, bindAddress={_config.BindAddress}");

        if (isService)
        {
            RunAsService();
        }
        else
        {
            RunConsole();
        }
    }

    private static void RunConsole()
    {
        try
        {
            StartServices();

            // Auto‑load persisted model on startup if specified
            if (!string.IsNullOrWhiteSpace(_config!.LmStudioLoadedModel))
            {
                Logger.Info($"Attempting to auto-load persisted model '{_config.LmStudioLoadedModel}' at startup.");
                var loaded = LmsCommandExecutor.LoadModel(_config.LmStudioLoadedModel);
                if (loaded)
                    ConfigManager.Save(_config!);
            }

            Console.WriteLine("===========================================");
            Console.WriteLine("  LmStudioServerAdmin is running");
            Console.WriteLine($"  Web interface: http://localhost:{_config!.Port}");
            Console.WriteLine("  Press Ctrl+C to stop");
            Console.WriteLine("===========================================");

            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Logger.Error($"Console mode error: {ex.Message}", ex);
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            StopServices();
        }
    }

    private static void RunAsService()
    {
        try
        {
            StartServices();
            Logger.Info("Running as Windows Service");

            // В режиме службы используем ManualWaitHandle для корректной остановки
            var eventHandled = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                eventHandled.Set();
            };
            eventHandled.WaitOne();
        }
        catch (Exception ex)
        {
            Logger.Error($"Service mode error: {ex.Message}", ex);
        }
        finally
        {
            StopServices();
        }
    }

    private static void StartServices()
    {
        // Запуск HTTP сервера
        _httpServer = new HttpServer(_config!);
        _httpServer.Start();
            // Start model list updater
            _modelListUpdater = new ModelListUpdater(_config!.LmStudioPort, TimeSpan.FromMinutes(5));

        // Запуск проверки статуса
        _statusChecker = new StatusChecker(status =>
        {
            Logger.Info($"Status changed: {status}");
        });

        // Первоначальная проверка статуса и автоопределение порта
        LmsCommandExecutor.GetStatus();
        LmsCommandExecutor.TryAutoDetectLmStudioPort();
    }

    public static void RestartServices()
    {
        StopServices();
        StartServices();
    }

    private static void StopServices()
    {
        Logger.Info("Shutting down...");
        _modelListUpdater?.Dispose();
        _statusChecker?.Dispose();
        _httpServer?.Dispose();
        Logger.Info("Shutdown complete");
    }
}
