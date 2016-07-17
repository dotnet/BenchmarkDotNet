using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Helpers
{
    // this class is used by our auto-generated benchmark program, 
    // keep it in mind if you want to do some renaming
    // you can finde the source code at Templates\BenchmarkProgram.txt
    public class BenchmarkEnvironmentInfo
    {
        public string Architecture { get; }

        public string Configuration { get; }

        public string ClrVersion { get; }

        public bool HasAttachedDebugger => Debugger.IsAttached;

        public bool HasRyuJit { get; }

        public bool IsServerGC { get; }

        public bool IsConcurrentGC { get; }

        protected BenchmarkEnvironmentInfo()
        {
            Architecture = RuntimeInformation.GetArchitecture();
            ClrVersion = RuntimeInformation.GetClrVersion();
            Configuration = RuntimeInformation.GetConfiguration();
            HasRyuJit = RuntimeInformation.HasRyuJit();
            IsServerGC = GCSettings.IsServerGC;
            IsConcurrentGC = GCSettings.LatencyMode != GCLatencyMode.Batch;
        }

        public static BenchmarkEnvironmentInfo GetCurrent() => new BenchmarkEnvironmentInfo();

        // ReSharper disable once UnusedMemberInSuper.Global
        public virtual IEnumerable<string> ToFormattedString()
        {
            yield return "Benchmark Process Environment Information:";
            yield return $"CLR={ClrVersion}, Arch={Architecture} {Configuration}{GetDebuggerFlag()}{GetJitFlag()}";
            yield return $"GC={GetGCLatencyMode()} {GetGCMode()}";
        }

        protected string GetJitFlag() => HasRyuJit ? " [RyuJIT]" : "";

        protected string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";

        protected string GetGCMode() => IsServerGC ? "Server" : "Workstation";

        protected string GetGCLatencyMode() => IsConcurrentGC ? "Concurrent" : "Non-concurrent";
    }

    public sealed class HostEnvironmentInfo : BenchmarkEnvironmentInfo
    {
        public static readonly CultureInfo MainCultureInfo;

        private static HostEnvironmentInfo Current;

        public string BenchmarkDotNetCaption { get; }

        public string BenchmarkDotNetVersion { get; }

        public string OsVersion { get; }

        /// <summary>
        /// is expensive to call (1s)
        /// </summary>
        public Lazy<string> ProcessorName { get; }

        public int ProcessorCount { get; }

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

        static HostEnvironmentInfo()
        {
            MainCultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            MainCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        private HostEnvironmentInfo()
        {
            BenchmarkDotNetCaption = GetBenchmarkDotNetCaption();
            BenchmarkDotNetVersion = GetBenchmarkDotNetVersion();
            OsVersion = RuntimeInformation.GetOsVersion();
            ProcessorName = new Lazy<string>(RuntimeInformation.GetProcessorName);
            ProcessorCount = Environment.ProcessorCount;
            ChronometerFrequency = Chronometer.Frequency;
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            JitModules = RuntimeInformation.GetJitModules();
            DotNetCliVersion = new Lazy<string>(DotNetCliCommandExecutor.GetDotNetCliVersion);
        }

        public new static HostEnvironmentInfo GetCurrent() => Current ?? (Current = new HostEnvironmentInfo());

        public double GetChronometerResolution() => Chronometer.BestClock.GetResolution(TimeUnit.Nanoseconds);

        public override IEnumerable<string> ToFormattedString()
        {
            yield return "Host Process Environment Information:";
            yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}";
            yield return $"OS={OsVersion}";
            yield return $"Processor={ProcessorName.Value}, ProcessorCount={ProcessorCount}";
            yield return $"Frequency={ChronometerFrequency} ticks, Resolution={GetChronometerResolution().ToTimeStr()}, Timer={HardwareTimerKind.ToString().ToUpper()}";
            yield return $"CLR={ClrVersion}, Arch={Architecture} {Configuration}{GetDebuggerFlag()}{GetJitFlag()}";
            yield return $"GC={GetGCLatencyMode()} {GetGCMode()}";
            yield return $"JitModules={JitModules}";
#if !CLASSIC
            yield return $"dotnet cli version: {DotNetCliVersion.Value}";
#endif
        }

        internal bool IsDotNetCliInstalled() => !string.IsNullOrEmpty(DotNetCliVersion.Value);

        private static string GetBenchmarkDotNetCaption() =>
            typeof(Benchmark).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyTitleAttribute>().First().Title;

        private static string GetBenchmarkDotNetVersion() =>
            typeof(Benchmark).GetTypeInfo().Assembly.GetName().Version + (GetBenchmarkDotNetCaption().EndsWith("-Dev") ? "+" : string.Empty);
    }
}