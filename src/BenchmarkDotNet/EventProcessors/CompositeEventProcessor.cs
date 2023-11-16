using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.EventProcessors
{
    internal sealed class CompositeEventProcessor : EventProcessor
    {
        private readonly HashSet<EventProcessor> eventProcessors;

        public CompositeEventProcessor(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            var eventProcessors = new HashSet<EventProcessor>();

            foreach (var info in benchmarkRunInfos)
                eventProcessors.AddRange(info.Config.GetEventProcessors());

            this.eventProcessors = eventProcessors;
        }

        public override void OnStartValidationStage()
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnStartValidationStage();
        }

        public override void OnValidationError(ValidationError validationError)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnValidationError(validationError);
        }

        public override void OnEndValidationStage()
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnEndValidationStage();
        }

        public override void OnStartBuildStage(IReadOnlyList<BuildPartition> partitions)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnStartBuildStage(partitions);
        }

        public override void OnBuildComplete(BuildPartition buildPartition, BuildResult buildResult)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnBuildComplete(buildPartition, buildResult);
        }

        public override void OnEndBuildStage()
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnEndBuildStage();
        }

        public override void OnStartRunStage()
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnStartRunStage();
        }

        public override void OnEndRunStage()
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnEndRunStage();
        }

        public override void OnStartRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnStartRunBenchmarksInType(type, benchmarks);
        }

        public override void OnEndRunBenchmarksInType(Type type, Summary summary)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnEndRunBenchmarksInType(type, summary);
        }

        public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnEndRunBenchmark(benchmarkCase, report);
        }

        public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase)
        {
            foreach (var eventProcessor in eventProcessors)
                eventProcessor.OnStartRunBenchmark(benchmarkCase);
        }
    }
}
