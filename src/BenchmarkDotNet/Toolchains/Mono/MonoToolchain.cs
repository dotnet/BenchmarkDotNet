using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Toolchains.Mono
{
    [PublicAPI]
    public class MonoToolchain : Toolchain, IEquatable<MonoToolchain>
    {
        private MonoToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        internal string CustomDotNetCliPath { get; }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
        {
            var runtimeIdentifier = CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier();
            return new MonoToolchain(settings.Name,
                        new MonoGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                        new MonoPublisher(
                            settings.CustomDotNetCliPath,
                            $"--self-contained -r {runtimeIdentifier} /p:UseMonoRuntime=true /p:RuntimeIdentifiers={runtimeIdentifier}"),
                        new DotNetCliExecutor(settings.CustomDotNetCliPath),
                        settings.CustomDotNetCliPath);
        }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
                return false;

            if (InvalidCliPath(CustomDotNetCliPath, benchmarkCase, logger))
                return false;

            if (benchmarkCase.Job.HasValue(EnvironmentMode.JitCharacteristic) && benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support CpuGroups (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support gcAllowVeryLargeObjects (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            var benchmarkAssembly = benchmarkCase.Descriptor.Type.Assembly;
            if (benchmarkAssembly.IsLinqPad())
            {
                logger.WriteLineError($"Currently CsProjMonoToolchain does not support LINQPad 6+. Please use {nameof(InProcessEmitToolchain)} instead. Benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is MonoToolchain typed && Equals(typed);

        public bool Equals(MonoToolchain other) => Generator.Equals(other.Generator);

        public override int GetHashCode() => Generator.GetHashCode();
    }
}
