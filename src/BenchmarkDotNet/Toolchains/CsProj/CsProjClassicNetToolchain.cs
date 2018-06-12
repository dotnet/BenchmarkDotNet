﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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

        private static readonly Dictionary<string, IToolchain> Toolchains = new Dictionary<string, IToolchain>
        {
            { "4.6", Net46 },
            { "4.6.1", Net461 },
            { "4.6.2", Net462 },
            { "4.7", Net47 },
            { "4.7.1", Net471 },
            { "4.7.2", Net472 }
        };

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

            return true;
        }

        private static IToolchain GetCurrentVersion()
        {
            if (!RuntimeInformation.IsWindows())
                return Net46; // we return .NET 4.6 which during validaiton will tell the user about lack of support
            
            // this logic is put to a separate method to avoid any assembly loading issues on non Windows systems
            var version = FrameworkVersionHelper.GetLatestNetDeveloperPackVersion();
            return Toolchains.TryGetValue(version, out var toolchain) ? toolchain : Default;
        }
    }
}