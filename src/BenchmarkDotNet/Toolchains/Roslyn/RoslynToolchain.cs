using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    /// <summary>
    /// Build a benchmark program with the Roslyn compiler.
    /// </summary>
    [PublicAPI]
    public class RoslynToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new RoslynToolchain();

        [PublicAPI]
        public RoslynToolchain() : base("Roslyn", new Generator(), Roslyn.Builder.Instance, new Executor())
        {
        }

        [PublicAPI]
        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            if (!RuntimeInformation.IsFullFramework)
            {
                yield return new ValidationError(true,
                    "The Roslyn toolchain is only supported on .NET Framework",
                    benchmarkCase);
            }

            if (benchmarkCase.Job.ResolveValue(GcMode.RetainVmCharacteristic, resolver))
            {
                yield return new ValidationError(true,
                    $"Currently App.config does not support RetainVM option, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic)
                && benchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, resolver) != InfrastructureMode.ReleaseConfigurationName)
            {
                yield return new ValidationError(true,
                    "The Roslyn toolchain does not allow to rebuild source project, so defining custom build configuration makes no sense",
                    benchmarkCase);
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic))
            {
                yield return new ValidationError(true,
                    "The Roslyn toolchain does not allow specifying NuGet package dependencies",
                    benchmarkCase);
            }
        }
    }
}