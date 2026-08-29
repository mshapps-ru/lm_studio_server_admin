#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Service
{
    public static class ServiceHelper
    {
        private const string ServiceName = "LmStudioServerAdmin";

        public static void CreateService()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                if (Path.GetExtension(exePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    exePath = Path.ChangeExtension(exePath, ".exe");

                IntPtr scmHandle = NativeMethods.OpenSCManager("", "",
                    NativeMethods.SC_MANAGER_ALL_ACCESS);
                if (scmHandle == IntPtr.Zero)
                    throw new Exception("OpenSCManager failed: " + Marshal.GetLastWin32Error());

                // Create service under NT SERVICE\LmStudioServerAdmin account
                var svcHandle = NativeMethods.CreateServiceA(
                    scmHandle,
                    ServiceName,
                    ServiceName,
                    NativeMethods.SERVICE_ALL_ACCESS,
                    NativeMethods.SERVICE_WIN32_OWN_PROCESS,
                    NativeMethods.SERVICE_AUTO_START,
                    NativeMethods.SERVICE_ERROR_NORMAL,
                    exePath,
                    "", // lpLoadOrderGroup
                    IntPtr.Zero, // lpdwTagId
                    "", // lpDependencies
                    $"NT SERVICE\\{ServiceName}",
                    "");

                if (svcHandle == IntPtr.Zero)
                    throw new Exception("CreateService failed: " + Marshal.GetLastWin32Error());

                // Configure restart on failure
                var actions = new NativeMethods.SC_ACTION[3];
                for (int i = 0; i < 3; i++)
                    actions[i] = new NativeMethods.SC_ACTION { Type = NativeMethods.SCAT_RESTART, Delay = 60000 };
                IntPtr actionPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.SC_ACTION>() * actions.Length);
                for (int i = 0; i < actions.Length; i++)
                    Marshal.StructureToPtr(actions[i], IntPtr.Add(actionPtr, i * Marshal.SizeOf<NativeMethods.SC_ACTION>()), false);

                var failActions = new NativeMethods.SERVICE_FAILURE_ACTIONS
                {
                    dwResetPeriod = 0,
                    lpRebootMsg = "",
                    lpCommand = "",
                    cActions = (uint)actions.Length,
                    lpsaActions = actionPtr
                };

                bool changed = NativeMethods.ChangeServiceConfig2A(svcHandle, NativeMethods.SERVICE_CONFIG_FAILURE_ACTIONS, ref failActions);
                Marshal.FreeHGlobal(actionPtr);
                if (!changed)
                    throw new Exception("ChangeServiceConfig2 failed: " + Marshal.GetLastWin32Error());

                NativeMethods.CloseServiceHandle(svcHandle);
                NativeMethods.CloseServiceHandle(scmHandle);

                Logger.Info("Windows service created and configured successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create Windows service: {ex.Message}", ex);
            }
        }

        public static void DeleteService()
        {
            try
            {
                IntPtr scmHandle = NativeMethods.OpenSCManager("", "",
                    NativeMethods.SC_MANAGER_ALL_ACCESS);
                if (scmHandle == IntPtr.Zero)
                    throw new Exception("OpenSCManager failed: " + Marshal.GetLastWin32Error());

                var svcHandle = NativeMethods.OpenService(scmHandle, ServiceName, NativeMethods.SERVICE_ALL_ACCESS);
                if (svcHandle == IntPtr.Zero)
                    throw new Exception("OpenService failed: " + Marshal.GetLastWin32Error());

                bool deleted = NativeMethods.DeleteService(svcHandle);
                if (!deleted)
                    throw new Exception("DeleteService failed: " + Marshal.GetLastWin32Error());

                NativeMethods.CloseServiceHandle(svcHandle);
                NativeMethods.CloseServiceHandle(scmHandle);

                Logger.Info("Windows service deleted.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete Windows service: {ex.Message}", ex);
            }
        }

        public static void StartService()
        {
            try
            {
                IntPtr scmHandle = NativeMethods.OpenSCManager("", "",
                    NativeMethods.SC_MANAGER_ALL_ACCESS);
                if (scmHandle == IntPtr.Zero)
                    throw new Exception("OpenSCManager failed: " + Marshal.GetLastWin32Error());

                var svcHandle = NativeMethods.OpenService(scmHandle, ServiceName, NativeMethods.SERVICE_START);
                if (svcHandle == IntPtr.Zero)
                    throw new Exception("OpenService failed: " + Marshal.GetLastWin32Error());

                bool started = NativeMethods.StartService(svcHandle, 0, Array.Empty<string>());
                if (!started)
                    throw new Exception("StartService failed: " + Marshal.GetLastWin32Error());

                NativeMethods.CloseServiceHandle(svcHandle);
                NativeMethods.CloseServiceHandle(scmHandle);

                Logger.Info("Windows service started.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start Windows service: {ex.Message}", ex);
            }
        }

        public static void StopService()
        {
            try
            {
                IntPtr scmHandle = NativeMethods.OpenSCManager("", "",
                    NativeMethods.SC_MANAGER_ALL_ACCESS);
                if (scmHandle == IntPtr.Zero)
                    throw new Exception("OpenSCManager failed: " + Marshal.GetLastWin32Error());

                var svcHandle = NativeMethods.OpenService(scmHandle, ServiceName, NativeMethods.SERVICE_STOP | NativeMethods.SERVICE_QUERY_STATUS);
                if (svcHandle == IntPtr.Zero)
                    throw new Exception("OpenService failed: " + Marshal.GetLastWin32Error());

                var status = new NativeMethods.SERVICE_STATUS();
                bool stopped = NativeMethods.ControlService(svcHandle, NativeMethods.SERVICE_CONTROL_STOP, ref status);
                if (!stopped)
                    throw new Exception("ControlService failed: " + Marshal.GetLastWin32Error());

                NativeMethods.CloseServiceHandle(svcHandle);
                NativeMethods.CloseServiceHandle(scmHandle);

                Logger.Info("Windows service stopped.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to stop Windows service: {ex.Message}", ex);
            }
        }

        internal static class NativeMethods
        {
            public const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
            public const uint SERVICE_ALL_ACCESS = 0xF01FF;
            public const uint SERVICE_START = 0x00000010;
            public const uint SERVICE_STOP = 0x00000100;
            public const uint SERVICE_QUERY_STATUS = 0x00000400;

            public const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
            public const int SERVICE_AUTO_START = 0x00000002;
            public const int SERVICE_ERROR_NORMAL = 0x00000001;

            public const int SCAT_RESTART = 1; // SC_ACTION_TYPE
            public const int SERVICE_CONFIG_FAILURE_ACTIONS = 2;
            public const uint SERVICE_CONTROL_STOP = 0x00000101;

            [StructLayout(LayoutKind.Sequential)]
            public struct SC_ACTION
            {
                public int Type;
                public uint Delay;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SERVICE_FAILURE_ACTIONS
            {
                public uint dwResetPeriod;
                public string lpRebootMsg;
                public string lpCommand;
                public uint cActions;
                public IntPtr lpsaActions;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SERVICE_STATUS
            {
                public uint dwServiceType;
                public uint dwCurrentState;
                public uint dwControlsAccepted;
                public uint dwWin32ExitCode;
                public uint dwServiceSpecificExitCode;
                public uint dwCheckPoint;
                public uint dwWaitHint;
            }

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, uint dwDesiredAccess);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr CreateServiceA(IntPtr hSCManager, string lpServiceName, string lpDisplayName,
                uint dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpBinaryPathName,
                string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool DeleteService(IntPtr hService);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool ControlService(IntPtr hService, uint dwControl, ref SERVICE_STATUS lpServiceStatus);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool ChangeServiceConfig2A(IntPtr hService, int dwInfoLevel, ref SERVICE_FAILURE_ACTIONS lpInfo);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool CloseServiceHandle(IntPtr hSCObject);
        }
    }
}
