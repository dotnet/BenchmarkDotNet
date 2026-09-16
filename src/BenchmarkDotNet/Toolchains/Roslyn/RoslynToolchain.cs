using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    public abstract class RoslynToolchain(string name, Runtime runtime, IBuilder builder, IExecutor executor)
        : Toolchain(name, runtime, RoslynGenerator.Instance, builder, executor)
    {
        public override async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            await foreach (var validationError in base.ValidateAsync(benchmarkCase, resolver).ConfigureAwait(false))
            {
                yield return validationError;
            }

            if (!(RuntimeInformation.IsFullFramework || RuntimeInformation.IsOldMono))
            {
                yield return new ValidationError(true,
                    $"{GetType().Name} is only supported on .NET Framework and legacy Mono",
                    benchmarkCase);
            }

            if (benchmarkCase.Job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic)
                && benchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, resolver) != InfrastructureMode.ReleaseConfigurationName)
            {
                yield return new ValidationError(true,
                    $"{GetType().Name} does not allow to rebuild source project, so defining custom build configuration makes no sense",
                    benchmarkCase);
            }
        }

        public override bool Equals(object? obj)
            => obj is RoslynToolchain other
            && other.GetType() == GetType()
            && Runtime.Equals(other.Runtime);

        public override int GetHashCode() => HashCode.Combine(GetType(), Runtime);
    }
}
