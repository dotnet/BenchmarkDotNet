using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    /// <summary>
    /// this toolchain is designed for the new .csprojs, to build .NET 4.x benchmarks from the context of .NET Core host process
    /// it does not work with the old .csprojs or project.json!
    /// </summary>
    [PublicAPI]
    public class CsProjClassicNetToolchain : Toolchain
    {
        [PublicAPI] public static readonly IToolchain Net46 = new CsProjClassicNetToolchain("net46");
        [PublicAPI] public static readonly IToolchain Net461 = new CsProjClassicNetToolchain("net461");
        [PublicAPI] public static readonly IToolchain Net462 = new CsProjClassicNetToolchain("net462");
        [PublicAPI] public static readonly IToolchain Net47 = new CsProjClassicNetToolchain("net47");
        [PublicAPI] public static readonly IToolchain Net471 = new CsProjClassicNetToolchain("net471");
        [PublicAPI] public static readonly IToolchain Net472 = new CsProjClassicNetToolchain("net472");
        private static readonly IToolchain Default = Net46; // the lowest version we support

        [PublicAPI]
        public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(GetCurrentVersion);

        private string targetFrameworkMoniker;

        private CsProjClassicNetToolchain(string targetFrameworkMoniker)
            : base($"CsProj{targetFrameworkMoniker}",
                new CsProjGenerator(targetFrameworkMoniker, platform => platform.ToConfig()),
                new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath: null),
                new Executor())
        {
            this.targetFrameworkMoniker = targetFrameworkMoniker;
        }

        public static IToolchain From(string targetFrameworkMoniker)
            => new CsProjClassicNetToolchain(targetFrameworkMoniker);

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (!RuntimeInformation.IsWindows())
            {
                logger.WriteLineError($"Classic .NET toolchain is supported only for Windows, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (benchmark.Job.HasValue(EnvMode.JitCharacteristic) && benchmark.Job.Env.Jit == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

        private static IToolchain GetCurrentVersion()
        {
            if (!RuntimeInformation.IsWindows())
                return Net46; // we return .NET 4.6 which during validaiton will tell the user about lack of support

            return GetCurrentVersionBasedOnWindowsRegistry(true);
        }

        // this logic is put to a separate method to avoid any assembly loading issues on non Windows systems
        // Reference Assemblies exists when Developer Pack is installed
        private static IToolchain GetCurrentVersionBasedOnWindowsRegistry(bool withDeveloperPack)
        {   
            using (var ndpKey = Microsoft.Win32.RegistryKey
                .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                if (ndpKey == null)
                    return Default;

                int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                // magic numbers come from https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
                if (releaseKey >= 461808 && (!withDeveloperPack || IsDeveloperPackInstalled(@"4.7.2")))
                    return Net472;
                if (releaseKey >= 461308 && (!withDeveloperPack || IsDeveloperPackInstalled(@"4.7.1")))
                    return Net471;
                if (releaseKey >= 460798 && (!withDeveloperPack || IsDeveloperPackInstalled(@"4.7")))
                    return Net47;
                if (releaseKey >= 394802 && (!withDeveloperPack || IsDeveloperPackInstalled(@"4.6.2")))
                    return Net462;
                if (releaseKey >= 394254 && (!withDeveloperPack || IsDeveloperPackInstalled(@"4.6.1")))
                    return Net461;

                return Default;
            }
        }

        private static bool IsDeveloperPackInstalled(string version) => Directory.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETFramework", 'v' + version));

        // TODO: Move to a better place
        [NotNull]
        internal static string GetCurrentNetFrameworkVersion()
        {
            var toolchain = GetCurrentVersionBasedOnWindowsRegistry(false) as CsProjClassicNetToolchain;
            if (toolchain == null)
                return "?";
            string version = toolchain.targetFrameworkMoniker.Replace("net", "");
            return string.Join(".", version.ToCharArray());
        }
    }
}