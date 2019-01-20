using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Roslyn;
using JetBrains.Annotations;
using System.IO;

namespace BenchmarkDotNet.Toolchains.Mono
{
    public class MonoAotToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new MonoAotToolchain();

        [PublicAPI]
        public MonoAotToolchain() : base("MonoAot", new Generator(), new MonoAotBuilder(), new Executor())
        {
        }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
            {
                return false;
            }

            if (!benchmarkCase.Job.Environment.HasValue(EnvironmentMode.RuntimeCharacteristic) || !(benchmarkCase.Job.Environment.Runtime is MonoRuntime))
            {
                logger.WriteLineError("The MonoAOT toolchain requires the Runtime property to be configured explicitly to an instance of MonoRuntime class");
                return false;
            }

            if ((benchmarkCase.Job.Environment.Runtime is MonoRuntime monoRuntime) && !string.IsNullOrEmpty(monoRuntime.MonoBclPath) && !Directory.Exists(monoRuntime.MonoBclPath))
            {
                logger.WriteLineError($"The MonoBclPath provided for MonoAOT toolchain: {monoRuntime.MonoBclPath} does NOT exist.");
                return false;
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic)
                && benchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, resolver) != InfrastructureMode.ReleaseConfigurationName)
            {
                logger.WriteLineError("The MonoAOT toolchain does not allow to rebuild source project, so defining custom build configuration makes no sense");
                return false;
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic))
            {
                logger.WriteLineError("The MonoAOT toolchain does not allow specifying NuGet package dependencies");
                return false;
            }

            return true;
        }
    }
}
