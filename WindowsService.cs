#pragma warning disable CA1416 // ServiceBase only on Windows
using System;
using System.ServiceProcess;
using LmStudioServerAdmin.Logging;
using System.Threading.Tasks;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Commands;
#pragma warning restore CA1416

namespace LmStudioServerAdmin
{
    public class WindowsService : ServiceBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA1416")]
        public WindowsService()
        {
            this.ServiceName = "LmStudioServerAdmin";
        }

        protected override void OnStart(string[] args)
        {
            // Load configuration for the service
            Program._config = ConfigManager.Load();
            LmsCommandExecutor.SetLmStudioPort(Program._config.LmStudioPort);
            Logger.Info($"Service: config loaded, user={Program._config.Username}, port={Program._config.Port}");

            Logger.Info("Windows Service starting.");
            try
            {
                Program.StartServices();
            // Auto-start LM Studio Server if configured
            if (Program._config != null && Program._config.LmStudioServerAutoStart)
            {
                try
                {
                    LmsCommandExecutor.StartServer();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to auto‑start LM Studio Server: {ex.Message}", ex);
                }
            }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start services: {ex.Message}", ex);
            }
        }

        protected override void OnStop()
        {
            Logger.Info("Windows Service stopping.");
            Program.StopServices();
        }
    }
}
