using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Engines
{
    public class EngineParameters
    {
        public IHost Host { get; set; }
        public Action<long> MainAction { get; set; }
        public Action Dummy1Action { get; set; }
        public Action Dummy2Action { get; set; }
        public Action Dummy3Action { get; set; }
        public Action<long> IdleAction { get; set; }
        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public Action GlobalSetupAction { get; set; } = null;
        public Action GlobalCleanupAction { get; set; } = null;
        public Action IterationSetupAction { get; set; } = null;
        public Action IterationCleanupAction { get; set; } = null;
        public IResolver Resolver { get; set; }
    }
}