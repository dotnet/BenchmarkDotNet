using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal static class EnvironmentHelper
    {
        private static string GetClrVersion()
        {
            var monoRuntimeType = Type.GetType("Mono.Runtime");
            var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
            if (monoDisplayName != null)
                return "Mono " + monoDisplayName.Invoke(null, null);
            return "MS.NET " + Environment.Version;
        }

        private static string GetArch()
        {
            return IntPtr.Size == 4 ? "32-bit" : "64-bit";
        }

        private static string GetConfiguration()
        {
            string configuration = "";
#if DEBUG
            configuration = "DEBUG";
#endif
            return configuration;
        }

        private static string GetDebuggerFlag()
        {
            return Debugger.IsAttached ? " [AttachedDebugger]" : "";
        }

        private static string GetJitFlag()
        {
            if (Type.GetType("Mono.Runtime") == null && IntPtr.Size == 8 && GetConfiguration() != "DEBUG")
                if (!new JitHelper().IsMsX64())
                    return " [RyuJIT]";
            return "";
        }

        public static string GetFullEnvironmentInfo()
        {
            var line1 = $"OS={GetOsVersion()}";
            var line2 = $"Processor={GetProcessorName()}, ProcessorCount={GetProcessorCount()}";
            var line3 = $"CLR={GetClrVersion()}, Arch={GetArch()}, {GetConfiguration()}{GetDebuggerFlag()}{GetJitFlag()}";
            return line1 + Environment.NewLine + line2 + Environment.NewLine + line3;
        }

        private static string GetOsVersion()
        {
            return Environment.OSVersion.ToString();
        }

        private static string GetProcessorCount()
        {
            return Environment.ProcessorCount.ToString();
        }

        private static string GetProcessorName()
        {
            var info = string.Empty;
            if (IsWindows())
            {
                try
                {
                    var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
                        info += moProcessor["name"]?.ToString();
                }
                catch (Exception)
                {
                }
            }
            else
                info = "?";
            return info;
        }

        private static bool IsWindows()
        {
            var platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32NT || platform == PlatformID.Win32S || platform == PlatformID.Win32Windows || platform == PlatformID.WinCE;
        }

        private class JitHelper
        {
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                var value = 0;
                for (int i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (int j = 0; j < 2 * step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }
    }
}