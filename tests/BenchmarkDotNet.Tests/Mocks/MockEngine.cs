using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mocks
{
    public class MockEngine : IEngine
    {
        private readonly ITestOutputHelper output;
        private readonly Func<IterationData, TimeInterval> measure;

        public EngineParameters Parameters { get; }

        internal MockEngine(ITestOutputHelper output, Job job, Func<IterationData, TimeInterval> measure)
        {
            this.output = output;
            this.measure = measure;
            Parameters = new EngineParameters
            {
                TargetJob = job,
                Dummy1Action = () => { },
                Dummy2Action = () => { },
                Dummy3Action = () => { },
                WorkloadActionUnroll = _ => { },
                WorkloadActionNoUnroll = _ => { },
                OverheadActionUnroll = _ => { },
                OverheadActionNoUnroll = _ => { },
                GlobalSetupAction = () => { },
                GlobalCleanupAction = () => { },
                IterationSetupAction = () => { },
                IterationCleanupAction = () => { },
            };
        }

        public void Dispose() { }

        private Measurement RunIteration(IterationData data)
        {
            double nanoseconds = measure(data).Nanoseconds;
            var measurement = new Measurement(1, data.mode, data.stage, data.index, data.invokeCount, nanoseconds);
            WriteLine(measurement.ToString());
            return measurement;
        }

        public RunResults Run() => default;

        internal List<Measurement> Run(EngineStage stage)
        {
            var measurements = stage.GetMeasurementList();
            while (stage.GetShouldRunIteration(measurements, out var iterationData))
            {
                var measurement = RunIteration(iterationData);
                measurements.Add(measurement);
            }
            return measurements;
        }

        public void WriteLine() => output.WriteLine("");
        public void WriteLine(string line) => output.WriteLine(line);

        public IResolver Resolver => new CompositeResolver(BenchmarkRunnerClean.DefaultResolver, EngineResolver.Instance);
    }
}