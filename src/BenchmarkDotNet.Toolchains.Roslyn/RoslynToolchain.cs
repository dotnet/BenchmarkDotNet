using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    /// <summary>
    /// Build a benchmark program with the Roslyn compiler.
    /// </summary>
    public class RoslynToolchain : Toolchain
    {
        [PublicAPI]
        public RoslynToolchain() : base("Roslyn", new Generator(), new Builder(), new Executor())
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (benchmark.Job.ResolveValue(GcMode.RetainVmCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently App.config does not support RetainVM option, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }
    }
}