using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains
{
    public class Toolchain : IToolchain
    {
        public string Name { get; }

        public IGenerator Generator { get; }

        public IBuilder Builder { get; }

        public IExecutor Executor { get; }

        public virtual bool IsInProcess => false;

        public Toolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor)
        {
            Name = name;
            Generator = generator;
            Builder = builder;
            Executor = executor;
        }

        public virtual async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            var runtime = benchmarkCase.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, resolver);
            var jit = benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver);
            if (!(runtime is MonoRuntime) && jit == Jit.Llvm)
            {
                yield return new ValidationError(true,
                    $"Llvm is supported only for Mono, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }

            if (runtime is MonoRuntime mono && !mono.IsDotNetBuiltIn && !benchmarkCase.GetToolchain().IsInProcess)
            {
                if (mono.CustomPath.IsBlank() && !HostEnvironmentInfo.GetCurrent().IsMonoInstalled.Value)
                {
                    yield return new ValidationError(true,
                        $"Mono is not installed or added to PATH, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                        benchmarkCase);
                }

                if (mono.CustomPath.IsNotBlank() && !File.Exists(mono.CustomPath))
                {
                    yield return new ValidationError(true,
                        $"We could not find Mono in provided path ({mono.CustomPath}), benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                        benchmarkCase);
                }
            }
        }

        public override string ToString() => Name;
    }
}