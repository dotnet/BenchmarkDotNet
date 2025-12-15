using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
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

        IConfig? config = GetConfig(ref args);

        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                                         .Run(args, config)
                                         .ToArray();

        if (summaries.HasError())
            return 1;

        return 0;
    }

    private static IConfig? GetConfig(ref string[] args)
    {
#if !DEBUG
        return null; // `DefaultConfig.Instance` is used.
#else
        bool isInProcess = args.Contains("--inProcess");
        if (isInProcess)
            args = args.Where(x => x != "--inProcess").ToArray();

        DebugConfig config = isInProcess
                            ? new DebugInProcessConfig()
                            : new DebugBuildConfig();

        return config.AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray())
                     .AddDiagnoser(
                         MemoryDiagnoser.Default,
#if NETCOREAPP3_0_OR_GREATER
                         new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayCompletedWorkItemCountWhenZero: false, displayLockContentionWhenZero: false)),
#endif
                         new ExceptionDiagnoser(new ExceptionDiagnoserConfig(displayExceptionsIfZeroValue: false))
                      )
                      .AddExporter(MarkdownExporter.Default)
                      .AddValidator(DefaultConfig.Instance.GetValidators().ToArray())
                      .WithArtifactsPath(DefaultConfig.Instance.ArtifactsPath);
#endif
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
