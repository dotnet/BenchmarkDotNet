using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
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

        [UsedImplicitly]
        public IHost Host { get; }

        public Job TargetJob { get; }
        public long OperationsPerInvoke { get; } = 1;

        [UsedImplicitly]
        public Action GlobalSetupAction { get; set; }

        [UsedImplicitly]
        public Action GlobalCleanupAction { get; set; }

        [UsedImplicitly]
        public bool IsDiagnoserAttached { get; set; }

        public Action<long> MainAction { get; } = _ => { };
        public Action<long> IdleAction { get; } = _ => { };

        [UsedImplicitly]
        public IEngineFactory Factory => null;

        public Measurement RunIteration(IterationData data)
        {
            double nanoseconds = measure(data).Nanoseconds;
            var measurement = new Measurement(1, data.IterationMode, data.Index, data.InvokeCount * OperationsPerInvoke, nanoseconds);
            WriteLine(measurement.ToOutputLine());
            return measurement;
        }

        public void PreAllocate() { }
        public void Jitting() { }

        public RunResults Run() => default(RunResults);

        public void WriteLine() => output.WriteLine("");
        public void WriteLine(string line) => output.WriteLine(line);

        public IResolver Resolver => new CompositeResolver(BenchmarkRunnerCore.DefaultResolver, EngineResolver.Instance);
    }
}