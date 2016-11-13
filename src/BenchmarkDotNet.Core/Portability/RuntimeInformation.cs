using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;

#if !CORE
using System.Management;
#endif

namespace BenchmarkDotNet.Portability
{
    public class RuntimeInformation
    {
        private const string Debug = "DEBUG";
        private const string Release = "RELEASE";
        internal const string Unknown = "?";

        internal static string ExecutableExtension => IsWindows() ? ".exe" : string.Empty;

        internal static string ScriptFileExtension => IsWindows() ? ".bat" : ".sh";

        internal static string GetArchitecture() => IntPtr.Size == 4 ? "32-bit" : "64-bit";

        internal static bool IsWindows()
        {
#if !CORE
            return new[] { PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE }
                .Contains(System.Environment.OSVersion.Platform);
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }

        private static bool IsMono() => Type.GetType("Mono.Runtime") != null;

        internal static string GetOsVersion()
        {
#if !CORE
            return System.Environment.OSVersion.ToString();
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

            return Unknown;
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
                  info = Regex.Replace(info.Replace("@", ""), @"\s+", " ");
                }
                catch (Exception)
                {
                }

                return info;
            }
#endif
            return Unknown; // TODO: verify if it is possible to get this for CORE
        }

        internal static string GetRuntimeVersion()
        {
            if (IsMono())
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                    return "Mono " + monoDisplayName.Invoke(null, null);
            }
#if CLASSIC
            return $"Clr {System.Environment.Version}";
#elif CORE
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#endif
        }

        internal static Runtime GetCurrentRuntime()
        {
#if CLASSIC
            return IsMono() ? Runtime.Mono : Runtime.Clr;
#elif CORE
            return Runtime.Core;
#endif
        }

        public static Platform GetCurrentPlatform() => IntPtr.Size == 4 ? Platform.X86 : Platform.X64;

        internal static string GetJitModules()
        {
#if !CORE
            return string.Join(";",
                Process.GetCurrentProcess().Modules
                    .OfType<ProcessModule>()
                    .Where(module => module.ModuleName.Contains("jit"))
                    .Select(module => Path.GetFileNameWithoutExtension(module.FileName) + "-v" + module.FileVersionInfo.ProductVersion));
#else
            return Unknown; // TODO: verify if it is possible to get this for CORE
#endif
        }

        internal static bool HasRyuJit()
        {
            return !IsMono()
                   && IntPtr.Size == 8
                   && GetConfiguration() != Debug
                   && !new JitHelper().IsMsX64();
        }

        internal static Jit GetCurrentJit()
        {
            return HasRyuJit() ? Jit.RyuJit : Jit.LegacyJit;
        }

        internal static IntPtr GetCurrentAffinity()
        {
            try
            {
                return Process.GetCurrentProcess().ProcessorAffinity;
            }
            catch (PlatformNotSupportedException)
            {
                return default(IntPtr);
            }
        }

        internal static string GetConfiguration()
        {
            bool? isDebug = Assembly.GetEntryAssembly().IsDebug();
            if (isDebug.HasValue == false)
            {
                return Unknown;
            }
            return isDebug.Value ? Debug : Release;
        }

        internal static string GetDotNetCliRuntimeIdentifier()
        {
#if CORE
            return Microsoft.DotNet.InternalAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
#else
// the Microsoft.DotNet.InternalAbstractions has no .NET 4.0 support, so we have to build it on our own
// code based on https://github.com/dotnet/cli/blob/f8631fa4b731d4c903dbe8b0b5e5332eee40ecae/src/Microsoft.DotNet.InternalAbstractions/RuntimeEnvironment.cs
            var version = System.Environment.OSVersion.Version;
            if (version.Major == 6)
            {
                if (version.Minor == 1)
                {
                    return "win7-x64";
                }
                else if (version.Minor == 2)
                {
                    return "win8-x64";
                }
                else if (version.Minor == 3)
                {
                    return "win81-x64";
                }
            }
            else if (version.Major == 10 && version.Minor == 0)
            {
                return "win10-x64";
            }

            return string.Empty; // Unknown version
#endif
        }

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
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