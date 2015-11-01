using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Reflection;

namespace BenchmarkDotNet
{
    public static class EnvironmentHelper
    {
        static EnvironmentHelper()
        {
            MainCultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            MainCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        public static readonly CultureInfo MainCultureInfo;

        private static bool IsMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private static string GetClrVersion()
        {
            if (IsMono())
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                    return "Mono " + monoDisplayName.Invoke(null, null);
            }
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

        public static string GetFullEnvironmentInfo(string clrHint = "", bool printDoubleSlash = true)
        {
            var prefix = printDoubleSlash ? "// " : string.Empty;
            var line1 = $"{prefix}{GetBenchmarkDotNetCaption()}=v{GetBenchmarkDotNetVersion()}";
            var line2 = $"{prefix}OS={GetOsVersion()}";
            var line3 = $"{prefix}Processor={GetProcessorName()}, ProcessorCount={GetProcessorCount()}";
            var line4 = $"{prefix}{clrHint}CLR={GetClrVersion()}, Arch={GetArch()} {GetConfiguration()}{GetDebuggerFlag()}{GetJitFlag()}";
            return string.Join(Environment.NewLine, new[] { line1, line2, line3, line4 });
        }

        private static string GetBenchmarkDotNetCaption()
        {
            return ((AssemblyTitleAttribute)typeof(BenchmarkRunner).Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;
        }

        private static string GetBenchmarkDotNetVersion()
        {
            return typeof(BenchmarkRunner).Assembly.GetName().Version + (GetBenchmarkDotNetCaption().EndsWith("-Dev") ? "+" : string.Empty);
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
            if (IsWindows() && !IsMono())
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