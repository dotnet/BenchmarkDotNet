using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if !CORE
using System.Management;
#endif

namespace BenchmarkDotNet.Portability
{
    internal class RuntimeInformation
    {
        internal static bool IsWindows()
        {
#if !CORE
            return new[] { PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE }
                .Contains(Environment.OSVersion.Platform);
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }

        internal static bool IsMono() => Type.GetType("Mono.Runtime") != null;

        internal static string GetOsVersion()
        {
#if !CORE
            return Environment.OSVersion.ToString();
#else
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX";
            }

            return "?";
#endif
        }

        internal static string GetProcessorName()
        {
#if !CORE
            if (IsWindows() && !IsMono())
            {
                var info = string.Empty;
                try
                {
                    var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
                        info += moProcessor["name"]?.ToString();
                }
                catch (Exception)
                {
                }

                return info;
            }
#endif
            return "?"; // TODO: verify if it is possible to get this for CORE
        }

        internal static string GetClrVersion()
        {
            if (IsMono())
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                    return "Mono " + monoDisplayName.Invoke(null, null);
            }
#if !CORE
            return "MS.NET " + Environment.Version;
#else
            return "CORE"; // TODO: verify if it is possible to get this for CORE
#endif
        }

    }
}