using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Models;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using Perfolizer.Helpers;
using Perfolizer.Horology;
using Perfolizer.Models;
using Perfolizer.Metrology;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Environments
{
    // this class is used by our auto-generated benchmark program,
    // keep it in mind if you want to do some renaming
    // you can find the source code at Templates\BenchmarkProgram.txt
    public class HostEnvironmentInfo : BenchmarkEnvironmentInfo
    {
        public const string BenchmarkDotNetCaption = "BenchmarkDotNet";

        // TODO: API to GlobalSetup the logger.
        /// <summary>
        /// Logger to use when there's no config available.
        /// </summary>
        public static ILogger FallbackLogger => ConsoleLogger.Default;

        private static HostEnvironmentInfo? current;

        public string BenchmarkDotNetVersion { get; protected set; }

        /// <summary>
        /// is expensive to call (1s)
        /// </summary>
        public Lazy<CpuInfo> Cpu { get; protected set; }

        public Lazy<OsInfo> Os { get; protected set; }

        /// <summary>
        /// .NET Core SDK version
        /// <remarks>It's expensive to call (creates new process by calling `dotnet --version`)</remarks>
        /// </summary>
        public Lazy<string> DotNetSdkVersion { get; protected set; }

        /// <summary>
        /// checks if Mono is installed
        /// <remarks>It's expensive to call (creates new process by calling `mono --version`)</remarks>
        /// </summary>
        public Lazy<bool> IsMonoInstalled { get; }

        /// <summary>
        /// The frequency of the timer as the number of ticks per second.
        /// </summary>
        [PublicAPI] public Frequency ChronometerFrequency { get; protected set; }

        [PublicAPI] public TimeInterval ChronometerResolution => ChronometerFrequency.ToResolution();

        public HardwareTimerKind HardwareTimerKind { get; protected set; }

        public Lazy<ICollection<Antivirus>> AntivirusProducts { get; }

        // TODO: Join with OsInfo
        public Lazy<VirtualMachineHypervisor?> VirtualMachineHypervisor { get; protected set; }

        protected HostEnvironmentInfo()
        {
            BenchmarkDotNetVersion = BenchmarkDotNetInfo.Instance.BrandVersion;
            ChronometerFrequency = Chronometer.Frequency;
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            DotNetSdkVersion = new Lazy<string>(DotNetCliCommandExecutor.GetDotNetSdkVersion);
            IsMonoInstalled = new Lazy<bool>(() => ProcessHelper.RunAndReadOutput("mono", "--version").IsNotBlank());
            AntivirusProducts = new Lazy<ICollection<Antivirus>>(RuntimeInformation.GetAntivirusProducts);
            VirtualMachineHypervisor = new Lazy<VirtualMachineHypervisor>(RuntimeInformation.GetVirtualMachineHypervisor);
            Os = new Lazy<OsInfo>(OsDetector.GetOs);
            Cpu = new Lazy<CpuInfo>(() => CpuDetector.CrossPlatform.Detect() ?? CpuInfo.Unknown);
        }

        public new static HostEnvironmentInfo GetCurrent() => current ??= new HostEnvironmentInfo();

        public override IEnumerable<string> ToFormattedString()
        {
            string? vmName = VirtualMachineHypervisor.Value?.Name;

            if (vmName.IsNotBlank())
                yield return $"{BenchmarkDotNetCaption} v{BenchmarkDotNetVersion}, {Os.Value.ToBrandString()} ({vmName})";
            else if (RuntimeInformation.IsRunningInContainer)
                yield return $"{BenchmarkDotNetCaption} v{BenchmarkDotNetVersion}, {Os.Value.ToBrandString()} (container)";
            else
                yield return $"{BenchmarkDotNetCaption} v{BenchmarkDotNetVersion}, {Os.Value.ToBrandString()}";

            yield return Cpu.Value.ToFullBrandName();
            if (HardwareTimerKind != HardwareTimerKind.Unknown)
            {
                string frequency = PerfolizerMeasurementFormatter.Instance.Format(
                    ChronometerFrequency.ToMeasurement(FrequencyUnit.Hz),
                    unitPresentation: UnitHelper.DefaultPresentation);
                string resolution = PerfolizerMeasurementFormatter.Instance.Format(
                    ChronometerResolution.ToMeasurement(),
                    format: "0.000",
                    unitPresentation: UnitHelper.DefaultPresentation);
                string timer = HardwareTimerKind.ToString().ToUpper();
                yield return $"Frequency: {frequency}, Resolution: {resolution}, Timer: {timer}";
            }

            if (RuntimeInformation.IsNetCore && IsDotNetCliInstalled())
            {
                // this wonderful version number contains words like "preview" and ... 5 segments, so it can not be parsed by Version.Parse. Example: "5.0.100-preview.8.20362.3"
                if (int.TryParse(new string(DotNetSdkVersion.Value.TrimStart().TakeWhile(char.IsDigit).ToArray()), out int major) && major >= 5)
                    yield return $".NET SDK {DotNetSdkVersion.Value}";
                else
                    yield return $".NET Core SDK {DotNetSdkVersion.Value}";
            }
        }

        [PublicAPI]
        public bool IsDotNetCliInstalled() => DotNetSdkVersion.Value.IsNotBlank();

        /// <summary>
        /// Return string representation of CPU and environment configuration including BenchmarkDotNet, OS and .NET version
        /// </summary>
        [PublicAPI]
        public static string GetInformation()
        {
            var hostEnvironmentInfo = GetCurrent();
            var sb = new StringBuilder();
            foreach (string infoLine in hostEnvironmentInfo.ToFormattedString())
            {
                sb.AppendLine(infoLine);
            }

            sb.AppendLine(Summary.BuildAllRuntimes(hostEnvironmentInfo, Array.Empty<BenchmarkReport>()));
            return sb.ToString();
        }

        internal BdnHostInfo ToPerfonar() => new()
        {
            Cpu = Cpu.Value,
            Os = Os.Value,
            RuntimeVersion = RuntimeVersion,
            HasAttachedDebugger = HasAttachedDebugger,
            HasRyuJit = HasRyuJit,
            Configuration = Configuration,
            DotNetSdkVersion = DotNetSdkVersion.Value,
            ChronometerFrequency = ChronometerFrequency.Hertz,
            HardwareTimerKind = HardwareTimerKind.ToString()
        };
    }
}