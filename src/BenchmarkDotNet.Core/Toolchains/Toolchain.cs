using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains
{
    public class Toolchain : IToolchain
    {
        public string Name { get; }

        public IGenerator Generator { get; }

        public IBuilder Builder { get; }

        public IExecutor Executor { get; }

        public RuntimeInformation RuntimeInformation { get; }

        public Toolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, RuntimeInformation runtimeInformation)
        {
            Name = name;
            Generator = generator;
            Builder = builder;
            Executor = executor;
            RuntimeInformation = runtimeInformation;
        }

        public virtual bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            var runtime = benchmark.Job.ResolveValue(EnvMode.RuntimeCharacteristic, resolver);
            var jit = benchmark.Job.ResolveValue(EnvMode.JitCharacteristic, resolver);
            if (!(runtime is MonoRuntime) && jit == Jit.Llvm)
            {
                logger.WriteLineError($"Llvm is supported only for Mono, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

        public override string ToString() => Name;
    }
}