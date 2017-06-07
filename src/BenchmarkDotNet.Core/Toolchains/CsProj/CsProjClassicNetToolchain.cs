using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using Microsoft.Win32;

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

        [PublicAPI]
        public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(GetCurrentVersion);

        private CsProjClassicNetToolchain(string targetFrameworkMoniker)
            : base($"CsProj{targetFrameworkMoniker}",
                new CsProjGenerator(targetFrameworkMoniker, platform => platform.ToConfig()),
                new CsProjBuilder(targetFrameworkMoniker),
                new Executor()) {}

        public static IToolchain From(string targetFrameworkMoniker)
            => new CsProjClassicNetToolchain(targetFrameworkMoniker);

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (benchmark.Job.ResolveValue(EnvMode.JitCharacteristic, resolver) == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

        private static IToolchain GetCurrentVersion()
        {
            try
            {
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
                {
                    int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                    // magic! https://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx
                    if (releaseKey >= 460798)
                        return Net47;
                    if (releaseKey >= 394802)
                        return Net462;
                    if (releaseKey >= 394254)
                        return Net461;

                    return Net461;
                }
            }
            catch (Exception e)
            {
                throw new Exception("You need to run on Windows with .NET 4.6+ installed", e);
            }
        }
    }
}