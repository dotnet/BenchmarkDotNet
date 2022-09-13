using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class RunStrategyTests : BenchmarkTestExecutor
    {
        public RunStrategyTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void RunStrategiesAreSupported()
        {
            var config = ManualConfig.CreateEmpty()
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddLogger(new OutputLogger(Output))
                .AddJob(new Job(Job.Dry) { Run = { RunStrategy = RunStrategy.ColdStart } })
                .AddJob(new Job(Job.Dry) { Run = { RunStrategy = RunStrategy.Monitoring } })
                .AddJob(new Job(Job.Dry) { Run = { RunStrategy = RunStrategy.Throughput } });

            var results = CanExecute<ModeBenchmarks>(config);

            Assert.Equal(6, results.BenchmarksCases.Count());

            Assert.Equal(1, results.BenchmarksCases.Count(b => b.Job.Run.RunStrategy == RunStrategy.ColdStart && b.Descriptor.WorkloadMethod.Name == "BenchmarkWithVoid"));
            Assert.Equal(1, results.BenchmarksCases.Count(b => b.Job.Run.RunStrategy == RunStrategy.ColdStart && b.Descriptor.WorkloadMethod.Name == "BenchmarkWithReturnValue"));

            Assert.Equal(1, results.BenchmarksCases.Count(b => b.Job.Run.RunStrategy == RunStrategy.Monitoring && b.Descriptor.WorkloadMethod.Name == "BenchmarkWithVoid"));
            Assert.Equal(1, results.BenchmarksCases.Count(b => b.Job.Run.RunStrategy == RunStrategy.Monitoring && b.Descriptor.WorkloadMethod.Name == "BenchmarkWithReturnValue"));

            Assert.Equal(1, results.BenchmarksCases.Count(b => b.Job.Run.RunStrategy == RunStrategy.Throughput && b.Descriptor.WorkloadMethod.Name == "BenchmarkWithVoid"));
            Assert.Equal(1, results.BenchmarksCases.Count(b => b.Job.Run.RunStrategy == RunStrategy.Throughput && b.Descriptor.WorkloadMethod.Name == "BenchmarkWithReturnValue"));

            Assert.Equal(6, results.Reports.Length);

            Assert.Single(results.Reports.Where(r => r.BenchmarkCase.Job.Run.RunStrategy == RunStrategy.ColdStart && r.BenchmarkCase.Descriptor.WorkloadMethod.Name == "BenchmarkWithVoid"));
            Assert.Single(results.Reports.Where(r => r.BenchmarkCase.Job.Run.RunStrategy == RunStrategy.ColdStart && r.BenchmarkCase.Descriptor.WorkloadMethod.Name == "BenchmarkWithReturnValue"));

            Assert.Single(results.Reports.Where(r => r.BenchmarkCase.Job.Run.RunStrategy == RunStrategy.Monitoring && r.BenchmarkCase.Descriptor.WorkloadMethod.Name == "BenchmarkWithVoid"));
            Assert.Single(results.Reports.Where(r => r.BenchmarkCase.Job.Run.RunStrategy == RunStrategy.Monitoring && r.BenchmarkCase.Descriptor.WorkloadMethod.Name == "BenchmarkWithReturnValue"));

            Assert.Single(results.Reports.Where(r => r.BenchmarkCase.Job.Run.RunStrategy == RunStrategy.Throughput && r.BenchmarkCase.Descriptor.WorkloadMethod.Name == "BenchmarkWithVoid"));
            Assert.Single(results.Reports.Where(r => r.BenchmarkCase.Job.Run.RunStrategy == RunStrategy.Throughput && r.BenchmarkCase.Descriptor.WorkloadMethod.Name == "BenchmarkWithReturnValue"));

            Assert.True(results.Reports.All(r => r.AllMeasurements.Any()));
        }

        public class ModeBenchmarks
        {
            [Benchmark]
            public void BenchmarkWithVoid() { }

            [Benchmark]
            public string BenchmarkWithReturnValue() => "okay";
        }
    }
}