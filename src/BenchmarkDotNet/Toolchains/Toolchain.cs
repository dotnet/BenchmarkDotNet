using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
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

        public virtual bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            var runtime = benchmark.Job.ResolveValue(EnvMode.RuntimeCharacteristic, resolver);
            var jit = benchmark.Job.ResolveValue(EnvMode.JitCharacteristic, resolver);
            if (!(runtime is MonoRuntime) && jit == Jit.Llvm)
            {
                logger.WriteLineError($"Llvm is supported only for Mono, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (runtime is MonoRuntime mono)
            {
                if (string.IsNullOrEmpty(mono.CustomPath) && !HostEnvironmentInfo.GetCurrent().IsMonoInstalled.Value)
                {
                    logger.WriteLineError($"Mono is not installed or added to PATH, benchmark '{benchmark.DisplayInfo}' will not be executed");
                    return false;
                }

                if (!string.IsNullOrEmpty(mono.CustomPath) && !File.Exists(mono.CustomPath))
                {
                    logger.WriteLineError($"We could not find Mono in provided path ({mono.CustomPath}), benchmark '{benchmark.DisplayInfo}' will not be executed");
                    return false;
                }
            }

            return true;
        }

        public override string ToString() => Name;
    }
}