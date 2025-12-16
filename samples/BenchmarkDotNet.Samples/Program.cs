using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System;
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
                     .AddExporter(MarkdownExporter.Default)
                     .AddValidator(DefaultConfig.Instance.GetValidators().ToArray())
                     .WithArtifactsPath(DefaultConfig.Instance.ArtifactsPath);
#endif
    }
}
