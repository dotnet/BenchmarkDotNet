using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Helpers
{
    public sealed class EnvironmentHelper
    {
        static EnvironmentHelper()
        {
            MainCultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            MainCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        public static readonly CultureInfo MainCultureInfo;

        public string BenchmarkDotNetCaption { get; set; }
        public string BenchmarkDotNetVersion { get; set; }
        public string OsVersion { get; set; }
        public string ProcessorName { get; set; }
        public int ProcessorCount { get; set; }
        public string ClrVersion { get; set; }
        public string Architecture { get; set; }
        public bool HasAttachedDebugger { get; set; }
        public bool HasRyuJit { get; set; }
        public string Configuration { get; set; }

        /// <summary>
        /// The frequency of the timer as the number of ticks per second.
        /// </summary>
        public long ChronometerFrequency { get; set; }

        public double GetChronometerResolution() => Chronometer.BestClock.GetResolution(TimeUnit.Nanoseconds);

        public static EnvironmentHelper GetCurrentInfo() => new EnvironmentHelper
        {
            BenchmarkDotNetCaption = GetBenchmarkDotNetCaption(),
            BenchmarkDotNetVersion = GetBenchmarkDotNetVersion(),
            OsVersion = GetOsVersion(),
            ProcessorName = GetProcessorName(),
            ProcessorCount = GetProcessorCount(),
            ClrVersion = GetClrVersion(),
            Architecture = GetArchitecture(),
            HasAttachedDebugger = GetHasAttachedDebugger(),
            HasRyuJit = GetHasRyuJit(),
            Configuration = GetConfiguration(),
            ChronometerFrequency = GetChronometerFrequency()
        };

        public string ToFormattedString(string clrHint = "")
        {
            var line1 = $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}";
            var line2 = $"OS={OsVersion}";
            var line3 = $"Processor={ProcessorName}, ProcessorCount={ProcessorCount}";
            var line4 = $"Frequency={ChronometerFrequency} ticks, Resolution={GetChronometerResolution().ToTimeStr()}";
            var line5 = $"{clrHint}CLR={ClrVersion}, Arch={Architecture} {Configuration}{GetDebuggerFlag()}{GetJitFlag()}";
            return string.Join(Environment.NewLine, line1, line2, line3, line4, line5);
        }

        private string GetJitFlag() => HasRyuJit ? " [RyuJIT]" : "";
        private string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";

        private static string GetBenchmarkDotNetCaption() =>
            typeof(BenchmarkRunner).Assembly().GetCustomAttributes<AssemblyTitleAttribute>(false).First().Title;

        private static string GetBenchmarkDotNetVersion() =>
            typeof(BenchmarkRunner).Assembly().GetName().Version + (GetBenchmarkDotNetCaption().EndsWith("-Dev") ? "+" : string.Empty);

        private static string GetOsVersion() => RuntimeInformation.GetOsVersion();

        private static string GetProcessorName() => RuntimeInformation.GetProcessorName();

        private static int GetProcessorCount() => Environment.ProcessorCount;

        private static string GetClrVersion() => RuntimeInformation.GetClrVersion();

        public static Runtime GetCurrentRuntime() => RuntimeInformation.GetCurrent();

        private static string GetArchitecture() => IntPtr.Size == 4 ? "32-bit" : "64-bit";

        private static bool GetHasAttachedDebugger() => Debugger.IsAttached;

        private static bool GetHasRyuJit()
        {
            if (Type.GetType("Mono.Runtime") == null && IntPtr.Size == 8 && GetConfiguration() != "DEBUG")
                if (!new JitHelper().IsMsX64())
                    return true;
            return false;
        }

        private static string GetConfiguration()
        {
            string configuration = "RELEASE";
#if DEBUG
            configuration = "DEBUG";
#endif
            return configuration;
        }

        private static long GetChronometerFrequency() => Chronometer.Frequency;

        public static bool IsMono() => RuntimeInformation.IsMono();

        public static bool IsWindows() => RuntimeInformation.IsWindows();

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