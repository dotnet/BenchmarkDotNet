using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
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
            Func<long, IClock, ValueTask<ClockSpan>> emptyAction = (_, _) => new(default(ClockSpan));
            Parameters = new EngineParameters
            {
                TargetJob = job,
                WorkloadActionUnroll = emptyAction,
                WorkloadActionNoUnroll = emptyAction,
                OverheadActionUnroll = emptyAction,
                OverheadActionNoUnroll = emptyAction,
                GlobalSetupAction = () => new(),
                GlobalCleanupAction = () => new(),
                IterationSetupAction = () => new(),
                IterationCleanupAction = () => new(),
                BenchmarkName = "",
                Host = default!,
                InProcessDiagnoserHandler = default!
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

        public ValueTask<RunResults> RunAsync() => new(default(RunResults));

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