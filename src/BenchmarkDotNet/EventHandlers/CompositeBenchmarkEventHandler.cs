using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.EventHandlers
{
    public class CompositeBenchmarkEventHandler : BenchmarkEventHandlerBase
    {
        private readonly IReadOnlyCollection<BenchmarkEventHandlerBase> eventHandlers;

        public CompositeBenchmarkEventHandler(IReadOnlyCollection<BenchmarkEventHandlerBase> eventHandlers)
        {
            this.eventHandlers = eventHandlers;
        }

        public override void OnStartValidationStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnStartValidationStage();
        }

        public override void OnValidationError(ValidationError validationError)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnValidationError(validationError);
        }

        public override void OnEndValidationStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnEndValidationStage();
        }

        public override void OnStartBuildStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnStartBuildStage();
        }

        public override void OnBuildFailed(BenchmarkCase benchmarkCase, BuildResult buildResult)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnBuildFailed(benchmarkCase, buildResult);
        }

        public override void OnEndBuildStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnEndBuildStage();
        }

        public override void OnStartRunStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnStartRunStage();
        }

        public override void OnEndRunStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnEndRunStage();
        }

        public override void OnStartRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnStartRunBenchmarksInType(type, benchmarks);
        }

        public override void OnEndRunBenchmarksInType(Type type, Summary summary)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnEndRunBenchmarksInType(type, summary);
        }

        public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnEndRunBenchmark(benchmarkCase, report);
        }

        public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.OnStartRunBenchmark(benchmarkCase);
        }
    }
}
