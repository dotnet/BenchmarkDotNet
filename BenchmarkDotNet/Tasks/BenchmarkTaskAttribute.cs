using System;

namespace BenchmarkDotNet.Tasks
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class BenchmarkTaskAttribute : Attribute
    {
        public BenchmarkTask Task { get; protected set; }

        public BenchmarkTaskAttribute(
            int processCount = 3,
            BenchmarkMode mode = BenchmarkMode.Throughput,
            BenchmarkPlatform platform = BenchmarkPlatform.HostPlatform,
            BenchmarkJitVersion jitVersion = BenchmarkJitVersion.HostJit,
            BenchmarkFramework framework = BenchmarkFramework.HostFramework,
            BenchmarkExecutor executor = BenchmarkExecutor.Classic,
            BenchmarkRuntime runtime = BenchmarkRuntime.Clr,
            int warmupIterationCount = 5,
            int targetIterationCount = 10
            )
        {
            Task = new BenchmarkTask(
                processCount,
                new BenchmarkConfiguration(mode, platform, jitVersion, framework, executor, runtime, warmupIterationCount, targetIterationCount));
        }
    }
}