using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
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
            public IEngine Create(EngineParameters engineParameters)
                => new CustomEngine(engineParameters);
        }

        public class CustomEngine(EngineParameters engineParameters) : IEngine
        {
            public RunResults Run()
            {
                engineParameters.GlobalSetupAction.Invoke();
                Console.WriteLine(EngineRunMessage);
                try
                {
                    return new RunResults(
                        [
                            new(1, IterationMode.Overhead, IterationStage.Actual, 1, 1, 1),
                            new(1, IterationMode.Workload, IterationStage.Actual, 1, 1, 1)
                        ],
                        OutlierMode.DontRemove,
                        default
                    );
                }
                finally
                {
                    engineParameters.GlobalCleanupAction.Invoke();
                }
            }
        }
    }
}