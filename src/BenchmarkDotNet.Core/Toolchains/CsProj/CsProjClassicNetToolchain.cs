using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
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
        private static readonly IToolchain Default = Net46; // the lowest version we support

        [PublicAPI]
        public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(GetCurrentVersion);

        private string targetFrameworkMoniker;

        private CsProjClassicNetToolchain(string targetFrameworkMoniker)
            : base($"CsProj{targetFrameworkMoniker}",
                new CsProjGenerator(targetFrameworkMoniker, platform => platform.ToConfig()),
                new CsProjBuilder(targetFrameworkMoniker, customDotNetCliPath: null),
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

#if NETCOREAPP1_1
            if (benchmark.Job.HasValue(InfrastructureMode.EnvironmentVariablesCharacteristic))
            {
                logger.WriteLineError($"ProcessStartInfo.EnvironmentVariables is avaialable for .NET Core 2.0, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }
#endif

            return true;
        }

        private static IToolchain GetCurrentVersion()
        {
            if (!RuntimeInformation.IsWindows())
                return Net46; // we return .NET 4.6 which during validaiton will tell the user about lack of support

            return GetCurrentVersionBasedOnWindowsRegistry();
        }

        // this logic is put to a separate method to avoid any assembly loading issues on non Windows systems
        private static IToolchain GetCurrentVersionBasedOnWindowsRegistry()
        {   
            using (var ndpKey = Microsoft.Win32.RegistryKey
                .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                if (ndpKey == null)
                    return Default;

                int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                // magic numbers come from https://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx
                if (releaseKey >= 460798 && Directory.Exists(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7"))
                    return Net47;
                if (releaseKey >= 394802 && Directory.Exists(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2"))
                    return Net462;
                if (releaseKey >= 394254 && Directory.Exists(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1"))
                    return Net461;

                return Default;
            }
        }

        // TODO: Move to a better place
        [NotNull]
        internal static string GetCurrentNetFrameworkVersion()
        {
            var toolchain = GetCurrentVersionBasedOnWindowsRegistry() as CsProjClassicNetToolchain;
            if (toolchain == null)
                return "?";
            string version = toolchain.targetFrameworkMoniker.Replace("net", "");
            return string.Join(".", version.ToCharArray());
        }
    }
}