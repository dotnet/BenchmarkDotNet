using System;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class TaskAttribute : Attribute
    {
        public BenchmarkTask Task { get; }

        public TaskAttribute(
            int runCount = 1,
            BenchmarkMode mode = BenchmarkMode.Throughput,
            BenchmarkPlatform platform = BenchmarkPlatform.CurrentPlatform,
            BenchmarkJitVersion jitVersion = BenchmarkJitVersion.CurrentJit,
            BenchmarkFramework framework = BenchmarkFramework.Current,
            int warmupIterationCount = 5,
            int targetIterationCount = 10
            )
        {
            Task = new BenchmarkTask(
                runCount,
                new BenchmarkConfiguration(mode, platform, jitVersion, framework),
                new BenchmarkSettings(warmupIterationCount, targetIterationCount));
        }
    }
}