using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Samples;

public class Program
{
    public static int Main(string[] args)
    {
#if DEBUG
        ConsoleLogger.Default.WriteLineWarning("Benchmark is executed with DEBUG configuration.");
        ConsoleLogger.Default.WriteLine();
#endif

        if (args.Length != 0)
        {
            ConsoleLogger.Default.WriteLine($"Start benchmarks with args: {string.Join(" ", args)}");
            ConsoleLogger.Default.WriteLine();
        }

        IConfig config;
#if DEBUG
        config = GetDebugConfig();
#else
        config = null; // `DefaultConfig.Instance` is used.
#endif

        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                                         .Run(args, config)
                                         .ToArray();

        if (summaries.HasError())
            return 1;

        return 0;
    }

    private static ManualConfig GetDebugConfig()
    {
        return DefaultConfig.Instance
                            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                            .WithOptions(ConfigOptions.StopOnFirstError)
                            .AddJob(
                                Job.Default
                                   .WithId("WithDebugConfiguration")
                                   .WithToolchain(InProcessEmitToolchain.Instance)
                                   .WithStrategy(RunStrategy.Monitoring)
                             );
    }
}

file static class ExtensionMethods
{
    public static bool HasError(this Summary[] summaries)
    {
        if (summaries.Length == 0)
        {
            var hashSet = new HashSet<string>(["--help", "--list", "--info", "--version"]);
            return !Environment.GetCommandLineArgs().Any(hashSet.Contains);
        }

        if (summaries.Any(x => x.HasCriticalValidationErrors))
            return true;

        return summaries.Any(x => x.Reports.Any(r => !r.Success));
    }
}
