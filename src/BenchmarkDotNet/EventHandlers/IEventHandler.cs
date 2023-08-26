using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.EventHandlers
{
    public interface IEventHandler
    {
        void HandleStartValidationStage();
        void HandleUnsupportedBenchmark(BenchmarkCase benchmarkCase);
        void HandleValidationError(ValidationError validationError);
        void HandleStartBuildStage();
        void HandleBuildFailed(BenchmarkCase benchmarkCase, BuildResult buildResult);
        void HandleStartRunStage();
        void HandleRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks);
        void HandleCompletedBenchmarksInType(Type type, Summary summary);
        void HandleRunBenchmark(BenchmarkCase benchmarkCase);
        void HandleCompletedBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report);
    }
}
