using System;
using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Environments
{
    // this class is used by our auto-generated benchmark program, 
    // keep it in mind if you want to do some renaming
    // you can finde the source code at Templates\BenchmarkProgram.txt
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
        /// is expensive to call (creates new process by calling dotnet --version)
        /// </summary>
        public Lazy<string> DotNetCliVersion { get; }

        /// <summary>
        /// The frequency of the timer as the number of ticks per second.
        /// </summary>
        public Frequency ChronometerFrequency { get; }

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
            ProcessorCount = System.Environment.ProcessorCount;
            ChronometerFrequency = Chronometer.Frequency;
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            JitModules = RuntimeInformation.GetJitModulesInfo();
            DotNetCliVersion = new Lazy<string>(DotNetCliCommandExecutor.GetDotNetCliVersion);
        }

        public new static HostEnvironmentInfo GetCurrent() => Current ?? (Current = new HostEnvironmentInfo());

        public TimeInterval GetChronometerResolution() => Chronometer.BestClock.GetResolution();

        public override IEnumerable<string> ToFormattedString()
        {
            yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}, OS={OsVersion}";
            yield return $"Processor={ProcessorName.Value}, ProcessorCount={ProcessorCount}";
            yield return $"Frequency={ChronometerFrequency}, Resolution={GetChronometerResolution()}, Timer={HardwareTimerKind.ToString().ToUpper()}";
#if !CLASSIC
            yield return $"dotnet cli version={DotNetCliVersion.Value}";
#endif
        }

        internal bool IsDotNetCliInstalled() => !string.IsNullOrEmpty(DotNetCliVersion.Value);

        private static string GetBenchmarkDotNetCaption() => "BenchmarkDotNet";
        private static string GetBenchmarkDotNetVersion() => BenchmarkDotNetInfo.FullVersion.Value;
    }
}