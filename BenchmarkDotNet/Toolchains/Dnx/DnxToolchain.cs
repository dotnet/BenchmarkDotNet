using System;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Dnx
{
    public class DnxToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new DnxToolchain();

        private DnxToolchain() 
            : base("Dnx",
                  new DotNetCliGenerator(
                      TargetFrameworkMonikerProvider,
                      extraDependencies: "\"frameworkAssemblies\": { \"System.Runtime\": \"4.0.20.0\" },",
                      platformProvider: platform => platform.ToConfig(),
                      imports: "\"portable-net45+win8\""),
                  new DotNetCliBuilder(TargetFrameworkMonikerProvider), 
                  new ClassicExecutor())
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger)
        {
            if (!EnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }

            return true;
        }

        private static string TargetFrameworkMonikerProvider(Framework framework)
        {
            switch (framework)
            {
                case Framework.Host: // we create dnx46 app so it can reference dnx451, dnx452 and dnx46 components as well
                case Framework.V46:
                    return "dnx46";
                case Framework.V451:
                    return "dnx451";
                case Framework.V452:
                    return "dnx452";
                default:
                    throw new NotSupportedException("Only Host, V451, V452 and V46 values are supported");
            }
        }
    }
}