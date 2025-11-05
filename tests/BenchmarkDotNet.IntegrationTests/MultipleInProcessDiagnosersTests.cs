using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class MultipleInProcessDiagnosersTests : BenchmarkTestExecutor
{
    public MultipleInProcessDiagnosersTests(ITestOutputHelper output) : base(output) { }

    public static IEnumerable<object[]> GetDiagnoserCombinations()
    {
        // Two diagnosers with NoOverhead
        yield return new object[]
        {
            new BaseMockInProcessDiagnoser[] { new MockInProcessDiagnoserNoOverhead(), new MockInProcessDiagnoser() },
            typeof(SimpleBenchmark),
            new[] { true, true }
        };

        // Two diagnosers with ExtraRun and NoOverhead
        yield return new object[]
        {
            new BaseMockInProcessDiagnoser[] { new MockInProcessDiagnoserExtraRun(), new MockInProcessDiagnoserNoOverhead() },
            typeof(SimpleBenchmark),
            new[] { true, true }
        };

        // Three diagnosers with varying run modes (None should not collect results)
        yield return new object[]
        {
            new BaseMockInProcessDiagnoser[] { new MockInProcessDiagnoserNoOverhead(), new MockInProcessDiagnoserExtraRun(), new MockInProcessDiagnoserNone() },
            typeof(SimpleBenchmark),
            new[] { true, true, false }
        };

        // Three different types
        yield return new object[]
        {
            new BaseMockInProcessDiagnoser[] { new MockInProcessDiagnoserNoOverhead(), new MockInProcessDiagnoser(), new MockInProcessDiagnoserExtraRun() },
            typeof(SimpleBenchmark),
            new[] { true, true, true }
        };

        // Multiple benchmarks
        yield return new object[]
        {
            new BaseMockInProcessDiagnoser[] { new MockInProcessDiagnoserNoOverhead(), new MockInProcessDiagnoserExtraRun() },
            typeof(MultipleBenchmarks),
            new[] { true, true }
        };
    }

    [Theory]
    [MemberData(nameof(GetDiagnoserCombinations))]
    public void MultipleInProcessDiagnosersWork(BaseMockInProcessDiagnoser[] diagnosers, Type benchmarkType, bool[] shouldHaveResults)
    {
        var logger = new OutputLogger(Output);
        var config = CreateInProcessConfig(logger);

        foreach (var diagnoser in diagnosers)
        {
            config = config.AddDiagnoser(diagnoser);
        }

        var summary = CanExecute(benchmarkType, config);

        for (int i = 0; i < diagnosers.Length; i++)
        {
            var diagnoser = diagnosers[i];
            var shouldHaveResult = shouldHaveResults[i];

            if (shouldHaveResult)
            {
                Assert.NotEmpty(diagnoser.Results);
                Assert.Equal(summary.BenchmarksCases.Length, diagnoser.Results.Count);
                Assert.All(diagnoser.Results.Values, result => Assert.Equal(diagnoser.ExpectedResult, result));
            }
            else
            {
                Assert.Empty(diagnoser.Results);
            }
        }

        // For multiple benchmarks, verify all benchmark methods are present
        if (benchmarkType == typeof(MultipleBenchmarks))
        {
            var benchmarkMethods = summary.BenchmarksCases.Select(bc => bc.Descriptor.WorkloadMethod.Name).ToList();
            Assert.Contains("Benchmark1", benchmarkMethods);
            Assert.Contains("Benchmark2", benchmarkMethods);
            Assert.Contains("Benchmark3", benchmarkMethods);
        }
    }

    private IConfig CreateInProcessConfig(OutputLogger logger)
    {
        return new ManualConfig()
            .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.DontLogOutput))
            .AddLogger(logger)
            .AddColumnProvider(DefaultColumnProviders.Instance);
    }

    public class SimpleBenchmark
    {
        private int counter;

        [Benchmark]
        public void BenchmarkMethod()
        {
            Interlocked.Increment(ref counter);
        }
    }

    public class MultipleBenchmarks
    {
        private int counter;

        [Benchmark]
        public void Benchmark1()
        {
            Interlocked.Increment(ref counter);
        }

        [Benchmark]
        public void Benchmark2()
        {
            Interlocked.Increment(ref counter);
        }

        [Benchmark]
        public void Benchmark3()
        {
            Interlocked.Increment(ref counter);
        }
    }
}
