using System;
using System.Threading.Tasks;
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

        // ReSharper disable once NotNullMemberIsNotInitialized
        public MockEngine(ITestOutputHelper output, Job job, Func<IterationData, TimeInterval> measure)
        {
            this.output = output;
            this.measure = measure;
            TargetJob = job;
        }

        public void Dispose() => GlobalSetupAction?.Invoke();

        [UsedImplicitly]
        public IHost Host { get; }

        public Job TargetJob { get; }
        public long OperationsPerInvoke { get; } = 1;

        [UsedImplicitly]
        public Func<ValueTask> GlobalSetupAction { get; set; }

        [UsedImplicitly]
        public Func<ValueTask> GlobalCleanupAction { get; set; }

        [UsedImplicitly]
        public bool IsDiagnoserAttached { get; set; }

        public Func<long, IClock, ValueTask<ClockSpan>> WorkloadAction { get; } = (invokeCount, clock) => default;
        public Func<long, IClock, ValueTask<ClockSpan>> OverheadAction { get; } = (invokeCount, clock) => default;

        [UsedImplicitly]
        public IEngineFactory Factory => null;

        public Measurement RunIteration(IterationData data)
        {
            double nanoseconds = measure(data).Nanoseconds;
            var measurement = new Measurement(1, data.IterationMode, data.IterationStage, data.Index, data.InvokeCount * OperationsPerInvoke, nanoseconds);
            WriteLine(measurement.ToString());
            return measurement;
        }

        public RunResults Run() => default;

        public void WriteLine() => output.WriteLine("");
        public void WriteLine(string line) => output.WriteLine(line);

        public IResolver Resolver => new CompositeResolver(BenchmarkRunnerClean.DefaultResolver, EngineResolver.Instance);
    }
}