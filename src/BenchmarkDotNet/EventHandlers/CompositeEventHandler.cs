using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.EventHandlers
{
    public class CompositeEventHandler : IEventHandler
    {
        private readonly IReadOnlyCollection<IEventHandler> eventHandlers;

        public CompositeEventHandler(IReadOnlyCollection<IEventHandler> eventHandlers)
        {
            this.eventHandlers = eventHandlers;
        }

        public void HandleStartValidationStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleStartValidationStage();
        }

        public void HandleUnsupportedBenchmark(BenchmarkCase benchmarkCase)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleUnsupportedBenchmark(benchmarkCase);
        }

        public void HandleValidationError(ValidationError validationError)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleValidationError(validationError);
        }

        public void HandleStartBuildStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleStartBuildStage();
        }

        public void HandleBuildFailed(BenchmarkCase benchmarkCase, BuildResult buildResult)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleBuildFailed(benchmarkCase, buildResult);
        }

        public void HandleStartRunStage()
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleStartRunStage();
        }

        public void HandleRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleRunBenchmarksInType(type, benchmarks);
        }

        public void HandleCompletedBenchmarksInType(Type type, Summary summary)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleCompletedBenchmarksInType(type, summary);
        }

        public void HandleCompletedBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleCompletedBenchmark(benchmarkCase, report);
        }

        public void HandleRunBenchmark(BenchmarkCase benchmarkCase)
        {
            foreach (var eventHandler in eventHandlers)
                eventHandler.HandleRunBenchmark(benchmarkCase);
        }
    }
}
