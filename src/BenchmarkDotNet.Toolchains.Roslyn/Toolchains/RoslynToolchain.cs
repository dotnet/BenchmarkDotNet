using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    /// <summary>
    /// Build a benchmark program with the Roslyn compiler.
    /// </summary>
    public class RoslynToolchain : Toolchain
    {
        /// <summary>
        /// Creates new instance of RoslynToolchain.
        /// </summary>
        [PublicAPI]
        public RoslynToolchain() : base("Classic", new RoslynGenerator(), new RoslynBuilder(), new Executor())
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