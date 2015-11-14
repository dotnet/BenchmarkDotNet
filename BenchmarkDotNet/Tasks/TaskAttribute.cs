using System;

namespace BenchmarkDotNet.Tasks
{
    [Obsolete("Use BenchmarkTask")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class TaskAttribute : BenchmarkTaskAttribute
    {
        public TaskAttribute(
            int processCount = 1,
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
                new BenchmarkConfiguration(mode, platform, jitVersion, framework, BenchmarkExecutor.Classic, BenchmarkRuntime.Clr, warmupIterationCount, targetIterationCount));
        }
    }
}