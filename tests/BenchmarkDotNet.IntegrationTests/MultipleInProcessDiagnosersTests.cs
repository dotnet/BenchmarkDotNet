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
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Xunit;
using Xunit.Abstractions;
using RunMode = BenchmarkDotNet.Diagnosers.RunMode;

namespace BenchmarkDotNet.IntegrationTests;

public class MultipleInProcessDiagnosersTests : BenchmarkTestExecutor
{
    public MultipleInProcessDiagnosersTests(ITestOutputHelper output) : base(output) { }

    private static readonly RunMode[] AllRunModes = { RunMode.NoOverhead, RunMode.ExtraRun, RunMode.None };

    private static IEnumerable<BaseMockInProcessDiagnoser[]> GetDiagnoserCombinations(int count)
    {
        if (count == 1)
        {
            foreach (var runMode in AllRunModes)
            {
                yield return [CreateDiagnoser(runMode, 0)];
            }
        }
        else if (count == 2)
        {
            foreach (var runMode1 in AllRunModes)
            {
                foreach (var runMode2 in AllRunModes)
                {
                    yield return [CreateDiagnoser(runMode1, 0), CreateDiagnoser(runMode2, 1)];
                }
            }
        }
        else if (count == 3)
        {
            foreach (var runMode1 in AllRunModes)
            {
                foreach (var runMode2 in AllRunModes)
                {
                    foreach (var runMode3 in AllRunModes)
                    {
                        yield return [CreateDiagnoser(runMode1, 0), CreateDiagnoser(runMode2, 1), CreateDiagnoser(runMode3, 2)];
                    }
                }
            }
        }
    }

    private static BaseMockInProcessDiagnoser CreateDiagnoser(RunMode runMode, int index)
    {
        return runMode switch
        {
            RunMode.NoOverhead => index == 0 ? new MockInProcessDiagnoserNoOverhead() : new MockInProcessDiagnoser(),
            RunMode.ExtraRun => new MockInProcessDiagnoserExtraRun(),
            RunMode.None => new MockInProcessDiagnoserNone(),
            _ => throw new ArgumentException($"Unsupported run mode: {runMode}")
        };
    }

    public static IEnumerable<object[]> GetTestCombinations()
    {
        var toolchains = new IToolchain[]
        {
            InProcessEmitToolchain.DontLogOutput,
            new InProcessNoEmitToolchain(TimeSpan.Zero, true),
            null // Default toolchain
        };

        var counts = new[] { 1, 2, 3 };

        foreach (var toolchain in toolchains)
        {
            foreach (var count in counts)
            {
                foreach (var diagnosers in GetDiagnoserCombinations(count))
                {
                    yield return new object[] { diagnosers, toolchain };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetTestCombinations))]
    public void MultipleInProcessDiagnosersWork(BaseMockInProcessDiagnoser[] diagnosers, IToolchain toolchain)
    {
        var logger = new OutputLogger(Output);
        var config = CreateConfig(logger, toolchain);

        foreach (var diagnoser in diagnosers)
        {
            config = config.AddDiagnoser(diagnoser);
        }

        var summary = CanExecute<SimpleBenchmark>(config);

        foreach (var diagnoser in diagnosers)
        {
            bool shouldHaveResults = diagnoser.DiagnoserRunMode != RunMode.None;

            if (shouldHaveResults)
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
    }

    private IConfig CreateConfig(OutputLogger logger, IToolchain toolchain)
    {
        var job = Job.Dry;
        if (toolchain != null)
        {
            job = job.WithToolchain(toolchain);
        }

        return new ManualConfig()
            .AddJob(job)
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
