using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using Perfolizer.Horology;

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
        public static ILogger FallbackLogger { get; } = ConsoleLogger.Default;

        private static HostEnvironmentInfo current;

        public string BenchmarkDotNetVersion { get; protected set; }

        /// <summary>
        /// Could be expensive
        /// </summary>
        public Lazy<string> OsVersion { get; protected set; }

        /// <summary>
        /// is expensive to call (1s)
        /// </summary>
        public Lazy<CpuInfo> CpuInfo { get; protected set; }

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

        public Lazy<VirtualMachineHypervisor> VirtualMachineHypervisor { get; protected set; }

        protected HostEnvironmentInfo()
        {
            BenchmarkDotNetVersion = BenchmarkDotNetInfo.FullVersion;
            OsVersion = new Lazy<string>(RuntimeInformation.GetOsVersion);
            CpuInfo = new Lazy<CpuInfo>(RuntimeInformation.GetCpuInfo);
            ChronometerFrequency = Chronometer.Frequency;
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            DotNetSdkVersion = new Lazy<string>(DotNetCliCommandExecutor.GetDotNetSdkVersion);
            IsMonoInstalled = new Lazy<bool>(() => !string.IsNullOrEmpty(ProcessHelper.RunAndReadOutput("mono", "--version")));
            AntivirusProducts = new Lazy<ICollection<Antivirus>>(RuntimeInformation.GetAntivirusProducts);
            VirtualMachineHypervisor = new Lazy<VirtualMachineHypervisor>(RuntimeInformation.GetVirtualMachineHypervisor);
        }

        public new static HostEnvironmentInfo GetCurrent() => current ?? (current = new HostEnvironmentInfo());

        public override IEnumerable<string> ToFormattedString()
        {
            string vmName = VirtualMachineHypervisor.Value?.Name;

            if (!string.IsNullOrEmpty(vmName))
                yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}, OS={OsVersion.Value}, VM={vmName}";
            else if (RuntimeInformation.IsRunningInContainer)
                yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}, OS={OsVersion.Value} (container)";
            else
                yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}, OS={OsVersion.Value}";

            yield return CpuInfoFormatter.Format(CpuInfo.Value);
            var cultureInfo = DefaultCultureInfo.Instance;
            if (HardwareTimerKind != HardwareTimerKind.Unknown)
                yield return $"Frequency={ChronometerFrequency}, Resolution={ChronometerResolution.ToString(cultureInfo)}, Timer={HardwareTimerKind.ToString().ToUpper()}";

            if (RuntimeInformation.IsNetCore && IsDotNetCliInstalled())
            {
                // this wonderfull version number contains words like "preview" and ... 5 segments so it can not be parsed by Version.Parse. Example: "5.0.100-preview.8.20362.3"
                if (int.TryParse(new string(DotNetSdkVersion.Value.TrimStart().TakeWhile(char.IsDigit).ToArray()), out int major) && major >= 5)
                    yield return $".NET SDK={DotNetSdkVersion.Value}";
                else
                    yield return $".NET Core SDK={DotNetSdkVersion.Value}";
            }
        }

        [PublicAPI]
        public bool IsDotNetCliInstalled() => !string.IsNullOrEmpty(DotNetSdkVersion.Value);

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
    }
}