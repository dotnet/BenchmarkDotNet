using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.EventProcessors
{
    public abstract class EventProcessor
    {
        public virtual void OnStartValidationStage() { }
        public virtual void OnValidationError(ValidationError validationError) { }
        public virtual void OnEndValidationStage() { }
        public virtual void OnStartBuildStage(IReadOnlyList<BuildPartition> partitions) { }
        public virtual void OnBuildComplete(BuildPartition partition, BuildResult buildResult) { }
        public virtual void OnEndBuildStage() { }
        public virtual void OnStartRunStage() { }
        public virtual void OnStartRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks) { }
        public virtual void OnEndRunBenchmarksInType(Type type, Summary summary) { }
        public virtual void OnStartRunBenchmark(BenchmarkCase benchmarkCase) { }
        public virtual void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report) { }
        public virtual void OnEndRunStage() { }
    }
}
