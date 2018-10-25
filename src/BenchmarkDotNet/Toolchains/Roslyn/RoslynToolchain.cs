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
    [PublicAPI]
    public class RoslynToolchain : Toolchain
    {
        public static IToolchain Instance = new RoslynToolchain();

        [PublicAPI]
        public RoslynToolchain() : base("Roslyn", new Generator(), new Builder(), new Executor())
        {
        }

        [PublicAPI]
        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
            {
                return false;
            }

            if (benchmarkCase.Job.ResolveValue(GcMode.RetainVmCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently App.config does not support RetainVM option, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic) 
                && benchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, resolver) != InfrastructureMode.ReleaseConfigurationName)
            {
                logger.WriteLineError("The Roslyn toolchain does not allow to rebuild source project, so defining custom build configuration makes no sense");
                return false;
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.NugetReferencesCharacteristic))
            {
                logger.WriteLineError("The Roslyn toolchain does not allow specifying Nuget package dependencies");
                return false;
            }

            return true;
        }
    }
}