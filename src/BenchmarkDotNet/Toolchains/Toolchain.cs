using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains
{
    public abstract class Toolchain(string name, Runtime runtime, IGenerator generator, IBuilder builder, IExecutor executor) : IToolchain
    {
        public Runtime Runtime { get; } = runtime;

        public IGenerator Generator { get; } = generator;

        public IBuilder Builder { get; } = builder;

        public IExecutor Executor { get; } = executor;

        public virtual bool IsInProcess => false;

        public virtual async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            var jit = benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver);
            if (jit == Jit.Llvm && Runtime is not (LegacyMonoRuntime or MonoCoreRuntime))
            {
                yield return new ValidationError(true,
                    $"Llvm is supported only for Mono, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }
        }

        public override string ToString() => Runtime.Version == null ? name : $"{name} {Runtime.Version}";
    }
}
