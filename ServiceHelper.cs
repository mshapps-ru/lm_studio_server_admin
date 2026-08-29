using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace LmStudioServerAdmin
{
    public static class ServiceHelper
    {
        public static string ExePath => Assembly.GetExecutingAssembly().Location;

        private static int RunSc(string arguments)
        {
            var psi = new ProcessStartInfo("sc", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) throw new Exception("Failed to start sc process");
            string outText = proc.StandardOutput.ReadToEnd();
            string errText = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                // include both stdout and stderr for better diagnostics
                throw new Exception($"sc {arguments} failed with exit code {proc.ExitCode}.\nSTDOUT: {outText}\nSTDERR: {errText}");
            return proc.ExitCode;
        }

        public static void CreateService()
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location;
            // Prefer .exe if available
            if (Path.GetExtension(exePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var exeCandidate = Path.ChangeExtension(exePath, ".exe");
                if (File.Exists(exeCandidate))
                    exePath = exeCandidate;
            }
            // Create service without extra arguments
            var args = $"create LmStudioServerAdmin binPath=\"{exePath}\" start=auto";
            RunSc(args);
        }

        public static void DeleteService() => RunSc("delete LmStudioServerAdmin");
        public static void StartService() => RunSc("start LmStudioServerAdmin");
        public static void StopService() => RunSc("stop LmStudioServerAdmin");
    }
}
