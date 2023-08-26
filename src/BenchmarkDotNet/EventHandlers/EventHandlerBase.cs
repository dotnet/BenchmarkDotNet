using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.EventHandlers
{
    public class EventHandlerBase : IEventHandler
    {
        public virtual void HandleBuildFailed(BenchmarkCase benchmarkCase, BuildResult buildResult)
        {
        }

        public virtual void HandleCompletedBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
        }

        public virtual void HandleCompletedBenchmarksInType(Type type, Summary summary)
        {
        }

        public virtual void HandleRunBenchmark(BenchmarkCase benchmarkCase)
        {
        }

        public virtual void HandleRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
        {
        }

        public virtual void HandleStartBuildStage()
        {
        }

        public virtual void HandleStartRunStage()
        {
        }

        public virtual void HandleStartValidationStage()
        {
        }

        public virtual void HandleUnsupportedBenchmark(BenchmarkCase benchmarkCase)
        {
        }

        public virtual void HandleValidationError(ValidationError validationError)
        {
        }
    }
}
