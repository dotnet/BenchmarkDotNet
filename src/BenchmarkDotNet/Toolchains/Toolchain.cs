using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
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

        public virtual IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
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
                if (string.IsNullOrEmpty(mono.CustomPath) && !HostEnvironmentInfo.GetCurrent().IsMonoInstalled.Value)
                {
                    yield return new ValidationError(true,
                        $"Mono is not installed or added to PATH, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                        benchmarkCase);
                }

                if (!string.IsNullOrEmpty(mono.CustomPath) && !File.Exists(mono.CustomPath))
                {
                    yield return new ValidationError(true,
                        $"We could not find Mono in provided path ({mono.CustomPath}), benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                        benchmarkCase);
                }
            }
        }

        internal static bool IsCliPathInvalid(string customDotNetCliPath, BenchmarkCase benchmarkCase, out ValidationError? validationError)
        {
            validationError = null;

            if (string.IsNullOrEmpty(customDotNetCliPath) && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                validationError = new ValidationError(true,
                    $"BenchmarkDotNet requires dotnet SDK to be installed or path to local dotnet cli provided in explicit way using `--cli` argument, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);

                return true;
            }

            if (!string.IsNullOrEmpty(customDotNetCliPath) && !File.Exists(customDotNetCliPath))
            {
                validationError = new ValidationError(true,
                    $"Provided custom dotnet cli path does not exist, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);

                return true;
            }

            return false;
        }

        public override string ToString() => Name;
    }
}
