﻿using System;
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
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty()
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddLogger(logger)
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

            string testLog = logger.GetLog();
            Assert.Contains("// ### Benchmark with void called ###", testLog);
            Assert.Contains("// ### Benchmark with return value called ###", testLog);
            Assert.DoesNotContain("No benchmarks found", logger.GetLog());
        }

        public class ModeBenchmarks
        {
            public bool FirstTime { get; set; }

            [GlobalSetup]
            public void GlobalSetup()
            {
                // Ensure we only print the diagnostic messages once per run in the tests, otherwise it fills up the output log!!
                FirstTime = true;
            }

            [Benchmark]
            public void BenchmarkWithVoid()
            {
                Thread.Sleep(10);
                if (FirstTime)
                {
                    Console.WriteLine("// ### Benchmark with void called ###");
                    FirstTime = false;
                }
            }

            [Benchmark]
            public string BenchmarkWithReturnValue()
            {
                Thread.Sleep(10);
                if (FirstTime)
                {
                    Console.WriteLine("// ### Benchmark with return value called ###");
                    FirstTime = false;
                }
                return "okay";
            }
        }
    }
}