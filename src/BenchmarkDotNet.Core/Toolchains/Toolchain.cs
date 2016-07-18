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

        public Toolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor)
        {
            Name = name;
            Generator = generator;
            Builder = builder;
            Executor = executor;
        }

        public virtual bool IsSupported(Benchmark benchmark, ILogger logger)
        {
            var runtime = benchmark.Job.Runtime == Runtime.Host ? RuntimeInformation.GetCurrent() : benchmark.Job.Runtime;
            if (runtime != Runtime.Mono && benchmark.Job.Jit == Jit.Llvm)
            {
                logger.WriteLineError($"Llvm is supported only for Mono, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }

            return true;
        }

        public override string ToString() => Name;
    }
}