﻿using System;
using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Properties;

namespace BenchmarkDotNet.Environments
{
    // this class is used by our auto-generated benchmark program, 
    // keep it in mind if you want to do some renaming
    // you can finde the source code at Templates\BenchmarkProgram.txt
    public sealed class HostEnvironmentInfo : BenchmarkEnvironmentInfo
    {
        public const string BenchmarkDotNetCaption = "BenchmarkDotNet";

        public static readonly CultureInfo MainCultureInfo;

        // TODO: API to setup the logger.
        /// <summary>
        /// Logger to use when there's no config available.
        /// </summary>
        public static ILogger FallbackLogger { get; } = ConsoleLogger.Default;

        private static HostEnvironmentInfo Current;

        public string BenchmarkDotNetVersion { get; }

        /// <summary>
        /// Could be expensive
        /// </summary>
        public Lazy<string> OsVersion { get; }

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
            BenchmarkDotNetVersion = GetBenchmarkDotNetVersion();
            OsVersion = new Lazy<string>(RuntimeInformation.GetOsVersion);
            ProcessorName = new Lazy<string>(RuntimeInformation.GetProcessorName);
            ProcessorCount = Environment.ProcessorCount;
            ChronometerFrequency = Chronometer.Frequency;
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            JitModules = RuntimeInformation.GetJitModulesInfo();
#if !UAP
            DotNetCliVersion = new Lazy<string>(Toolchains.DotNetCli.DotNetCliCommandExecutor.GetDotNetCliVersion);
#else
            DotNetCliVersion = new Lazy<string>(() => string.Empty);
#endif
        }

        public new static HostEnvironmentInfo GetCurrent() => Current ?? (Current = new HostEnvironmentInfo());

        public TimeInterval GetChronometerResolution() => Chronometer.BestClock.GetResolution();

        public override IEnumerable<string> ToFormattedString()
        {
            yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}, OS={OsVersion.Value}";
            yield return $"Processor={ProcessorName.Value}, ProcessorCount={ProcessorCount}";
            yield return $"Frequency={ChronometerFrequency}, Resolution={GetChronometerResolution()}, Timer={HardwareTimerKind.ToString().ToUpper()}";
#if !CLASSIC
            yield return $"dotnet cli version={DotNetCliVersion.Value}";
#endif
        }

        internal bool IsDotNetCliInstalled() => !string.IsNullOrEmpty(DotNetCliVersion.Value);

        private static string GetBenchmarkDotNetVersion() => BenchmarkDotNetInfo.FullVersion;
    }
}