using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Helpers
{
    public sealed class EnvironmentInfo
    {
        public static readonly CultureInfo MainCultureInfo;

        private static EnvironmentInfo Current;

        public string BenchmarkDotNetCaption { get; }

        public string BenchmarkDotNetVersion { get; }

        public string OsVersion { get; }

        public string ProcessorName { get; }

        public int ProcessorCount { get; }

        public string ClrVersion { get; }

        public string Architecture { get; }

        public bool HasAttachedDebugger { get; }

        public bool HasRyuJit { get; }

        public string Configuration { get; }

        public string JitModules { get; }

        /// <summary>
        /// is expensive to call (creates new process)
        /// </summary>
        public Lazy<string> DotNetCliVersion { get; }

        /// <summary>
        /// The frequency of the timer as the number of ticks per second.
        /// </summary>
        public long ChronometerFrequency { get; }

        public HardwareTimerKind HardwareTimerKind { get; }

        static EnvironmentInfo()
        {
            MainCultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            MainCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        private EnvironmentInfo()
        {
            BenchmarkDotNetCaption = GetBenchmarkDotNetCaption();
            BenchmarkDotNetVersion = GetBenchmarkDotNetVersion();
            OsVersion = RuntimeInformation.GetOsVersion();
            ProcessorName = RuntimeInformation.GetProcessorName();
            ProcessorCount = Environment.ProcessorCount;
            ClrVersion = RuntimeInformation.GetClrVersion();
            Architecture = GetArchitecture();
            HasAttachedDebugger = Debugger.IsAttached;
            HasRyuJit = RuntimeInformation.HasRyuJit();
            Configuration = RuntimeInformation.GetConfiguration();
            ChronometerFrequency = Chronometer.Frequency;
            JitModules = RuntimeInformation.GetJitModules();
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            DotNetCliVersion = new Lazy<string>(DotNetCliCommandExecutor.GetDotNetCliVersion);
        }

        // this method is called from our auto-generated benchmark program, keep it in mind if you want to do some renaming
        public static EnvironmentInfo GetCurrent() => Current ?? (Current = new EnvironmentInfo());

        public double GetChronometerResolution() => Chronometer.BestClock.GetResolution(TimeUnit.Nanoseconds);

        /// <param name="includeDotnetCliVersion">disabled by default to avoid perf hit for auto-generated program that also calls this method</param>
        public IEnumerable<string> ToFormattedString(string clrHint = "", bool includeDotnetCliVersion = false)
        {
            yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}";
            yield return $"OS={OsVersion}";
            yield return $"Processor={ProcessorName}, ProcessorCount={ProcessorCount}";
            yield return $"Frequency={ChronometerFrequency} ticks, Resolution={GetChronometerResolution().ToTimeStr()}, Timer={HardwareTimerKind.ToString().ToUpper()}";
            yield return $"{clrHint}CLR={ClrVersion}, Arch={Architecture} {Configuration}{GetDebuggerFlag()}{GetJitFlag()}";
            yield return $"JitModules={JitModules}";

#if !CLASSIC
            if(includeDotnetCliVersion)
            {
                yield return DotNetCliVersion.Value;
            }
#endif
        }

        internal bool IsDotNetCliInstalled() => !string.IsNullOrEmpty(DotNetCliVersion.Value);

        private string GetJitFlag() => HasRyuJit ? " [RyuJIT]" : "";

        private string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";

        private static string GetBenchmarkDotNetCaption() =>
            typeof(BenchmarkRunner).Assembly().GetCustomAttributes<AssemblyTitleAttribute>(false).First().Title;

        private static string GetBenchmarkDotNetVersion() =>
            typeof(BenchmarkRunner).Assembly().GetName().Version + (GetBenchmarkDotNetCaption().EndsWith("-Dev") ? "+" : string.Empty);

        private static string GetArchitecture() => IntPtr.Size == 4 ? "32-bit" : "64-bit";
    }
}