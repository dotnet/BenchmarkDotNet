using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples.Sandbox;

internal class Program
{
    public static int Main(string[] args)
    {
        var logger = ConsoleLogger.Default;
#if DEBUG
        logger.WriteLineWarning("Benchmark is executed with DEBUG configuration.");
        logger.WriteLine();
#endif

        if (args.Length != 0)
            logger.WriteLine($"Start benchmarks with args: {string.Join(" ", args)}");

        try
        {
            // Get benchmark config
            var config = AssemblyConfigProvider.GetConfig();
            logger.WriteLine($"Selected benchmark config: {config.GetType().Name}");
            logger.WriteLine();

            // Run benchmarks
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                                             .Run(args, config)
                                             .ToArray();

            // Check benchmark results.
            if (summaries.HasError())
            {
                logger.WriteLine();
                logger.WriteLineError("Failed to run benchmarks.");
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            logger.WriteLineError(ex.ToString());
            return 1;
        }
    }
}
