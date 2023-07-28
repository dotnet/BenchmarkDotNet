using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Roslyn;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Mono
{
    public class MonoAotToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new MonoAotToolchain();

        [PublicAPI]
        public MonoAotToolchain() : base("MonoAot", new Generator(), new MonoAotBuilder(), new Executor())
        {
        }

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            if (!benchmarkCase.Job.Environment.HasValue(EnvironmentMode.RuntimeCharacteristic) || benchmarkCase.Job.Environment.Runtime is not MonoRuntime)
            {
                yield return new ValidationError(true,
                    "The MonoAOT toolchain requires the Runtime property to be configured explicitly to an instance of MonoRuntime class",
                    benchmarkCase);
            }

            if ((benchmarkCase.Job.Environment.Runtime is MonoRuntime monoRuntime) && !string.IsNullOrEmpty(monoRuntime.MonoBclPath) && !Directory.Exists(monoRuntime.MonoBclPath))
            {
                yield return new ValidationError(true,
                    $"The MonoBclPath provided for MonoAOT toolchain: {monoRuntime.MonoBclPath} does NOT exist.",
                    benchmarkCase);
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic)
                && benchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, resolver) != InfrastructureMode.ReleaseConfigurationName)
            {
                yield return new ValidationError(true,
                    "The MonoAOT toolchain does not allow to rebuild source project, so defining custom build configuration makes no sense",
                    benchmarkCase);
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic))
            {
                yield return new ValidationError(true,
                    "The MonoAOT toolchain does not allow specifying NuGet package dependencies",
                    benchmarkCase);
            }
        }
    }
}