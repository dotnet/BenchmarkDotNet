using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Characteristics;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CustomEngineTests : BenchmarkTestExecutor
    {
        private const string GlobalSetupMessage = "// GlobalSetup got called";
        private const string EngineRunMessage = "// EngineRun got called";
        private const string GlobalCleanupMessage = "// GlobalCleanup got called";

        public CustomEngineTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CustomEnginesAreSupported()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(new Job(Job.Dry) { Infrastructure = { EngineFactory = new CustomFactory() } });

            var summary = CanExecute<SimpleBenchmark>(config, fullValidation: false);

            IReadOnlyList<string> standardOutput = GetSingleStandardOutput(summary);

            Assert.Contains(GlobalSetupMessage, standardOutput);
            Assert.Contains(EngineRunMessage, standardOutput);
            Assert.Contains(GlobalCleanupMessage, standardOutput);
        }

        public class SimpleBenchmark
        {
            [GlobalSetup]
            public void Setup() => Console.WriteLine(GlobalSetupMessage);

            [GlobalCleanup]
            public void Cleanup() => Console.WriteLine(GlobalCleanupMessage);

            [Benchmark]
            public void Empty() { }
        }

        public class CustomFactory : IEngineFactory
        {
            public IEngine CreateReadyToRun(EngineParameters engineParameters)
            {
                var engine = new CustomEngine
                {
                    GlobalCleanupAction = engineParameters.GlobalCleanupAction,
                    GlobalSetupAction = engineParameters.GlobalSetupAction
                };

                engine.GlobalSetupAction?.Invoke(); // engine factory is now supposed to create an engine which is ready to run (hence the method name change)

                return engine;
            }
        }

        public class CustomEngine : IEngine
        {
            public RunResults Run()
            {
                Console.WriteLine(EngineRunMessage);

                return new RunResults(
                    new List<Measurement> { new Measurement(1, IterationMode.Overhead, IterationStage.Actual, 1, 1, 1) },
                    new List<Measurement> { new Measurement(1, IterationMode.Workload, IterationStage.Actual, 1, 1, 1) },
                    OutlierMode.DontRemove,
                    default,
                    default);
            }

            public void Dispose() => GlobalCleanupAction?.Invoke();

            public IHost Host { get; }
            public void WriteLine() { }
            public void WriteLine(string line) { }
            public Job TargetJob { get; }
            public long OperationsPerInvoke { get; }
            public Action GlobalSetupAction { get; set; }
            public Action GlobalCleanupAction { get; set; }
            public Action<long> WorkloadAction { get; }
            public Action<long> OverheadAction { get; }
            public IResolver Resolver { get; }

            public Measurement RunIteration(IterationData data) { throw new NotImplementedException(); }
        }
    }
}