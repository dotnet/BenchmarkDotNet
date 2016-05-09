using System;
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

        public virtual bool IsSupported(Benchmark benchmark, ILogger logger) => true;

        public override string ToString() => Name;

        public static IToolchain GetToolchain(IJob job) => job.Toolchain ?? GetToolchain(job.Runtime);

        internal static IToolchain GetToolchain(Runtime runtime)
        {
            switch (runtime)
            {
                case Runtime.Host:
                    return GetToolchain(RuntimeInformation.GetCurrent());
                case Runtime.Clr:
                case Runtime.Mono:
                    return Classic.ClassicToolchain.Instance;
                case Runtime.Dnx:
                    return Dnx.DnxToolchain.Instance;
                case Runtime.Core:
                    return Core.CoreToolchain.Instance;
            }

            throw new NotSupportedException("Runtime not supported");
        }
    }
}