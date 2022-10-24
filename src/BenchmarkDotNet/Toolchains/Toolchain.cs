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

        public virtual bool IsInProcess => false;

        public Toolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor)
        {
            Name = name;
            Generator = generator;
            Builder = builder;
            Executor = executor;
        }

        public virtual bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            var runtime = benchmarkCase.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, resolver);
            var jit = benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver);
            if (!(runtime is MonoRuntime) && jit == Jit.Llvm)
            {
                logger.WriteLineError($"Llvm is supported only for Mono, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            if (runtime is MonoRuntime mono && !mono.IsDotNetBuiltIn && !benchmarkCase.GetToolchain().IsInProcess)
            {
                if (string.IsNullOrEmpty(mono.CustomPath) && !HostEnvironmentInfo.GetCurrent().IsMonoInstalled.Value)
                {
                    logger.WriteLineError($"Mono is not installed or added to PATH, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                    return false;
                }

                if (!string.IsNullOrEmpty(mono.CustomPath) && !File.Exists(mono.CustomPath))
                {
                    logger.WriteLineError($"We could not find Mono in provided path ({mono.CustomPath}), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                    return false;
                }
            }

            return true;
        }

        internal static bool InvalidCliPath(string customDotNetCliPath, BenchmarkCase benchmarkCase, ILogger logger)
        {
            if (string.IsNullOrEmpty(customDotNetCliPath) && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli to be installed or path to local dotnet cli provided in explicit way using `--cli` argument, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return true;
            }

            if (!string.IsNullOrEmpty(customDotNetCliPath) && !File.Exists(customDotNetCliPath))
            {
                logger.WriteLineError($"Provided custom dotnet cli path does not exist, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return true;
            }

            return false;
        }

        public override string ToString() => Name;
    }
}