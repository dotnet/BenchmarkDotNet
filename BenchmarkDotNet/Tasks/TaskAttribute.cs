using System;

namespace BenchmarkDotNet.Tasks
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class TaskAttribute : Attribute
    {
        public BenchmarkTask Task { get; }

        public TaskAttribute(
            int processCount = 3,
            BenchmarkMode mode = BenchmarkMode.Throughput,
            BenchmarkPlatform platform = BenchmarkPlatform.HostPlatform,
            BenchmarkJitVersion jitVersion = BenchmarkJitVersion.HostJit,
            BenchmarkFramework framework = BenchmarkFramework.HostFramework,
            int warmupIterationCount = 5,
            int targetIterationCount = 10
            )
        {
            Task = new BenchmarkTask(
                processCount,
                new BenchmarkConfiguration(mode, platform, jitVersion, framework),
                new BenchmarkSettings(warmupIterationCount, targetIterationCount));
        }
    }
}