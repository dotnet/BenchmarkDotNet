﻿using System;
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
        internal const string DefaultConfiguration = "Release";

        [PublicAPI] public static readonly IToolchain Net46 = new CsProjClassicNetToolchain("net46", DefaultConfiguration);
        [PublicAPI] public static readonly IToolchain Net461 = new CsProjClassicNetToolchain("net461", DefaultConfiguration);
        [PublicAPI] public static readonly IToolchain Net462 = new CsProjClassicNetToolchain("net462", DefaultConfiguration);
        [PublicAPI] public static readonly IToolchain Net47 = new CsProjClassicNetToolchain("net47", DefaultConfiguration);
        private static readonly IToolchain Default = Net46; // the lowest version we support

        [PublicAPI]
        public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(GetCurrentVersion);

        private CsProjClassicNetToolchain(string targetFrameworkMoniker, string configuration)
            : base($"CsProj{targetFrameworkMoniker}",
                new CsProjGenerator(targetFrameworkMoniker, platform => platform.ToConfig(), configuration),
                new CsProjBuilder(targetFrameworkMoniker, configuration),
                new Executor()) {}

        public static IToolchain From(string targetFrameworkMoniker, string configuration)
            => new CsProjClassicNetToolchain(targetFrameworkMoniker, configuration);

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

            if (benchmark.Job.ResolveValue(EnvMode.JitCharacteristic, resolver) == Jit.LegacyJit)
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
                if (releaseKey >= 460798)
                    return Net47;
                if (releaseKey >= 394802)
                    return Net462;
                if (releaseKey >= 394254)
                    return Net461;

                return Default;
            }
        }
    }
}