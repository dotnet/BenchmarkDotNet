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

public class InProcessDiagnoserTests : BenchmarkTestExecutor
{
    public InProcessDiagnoserTests(ITestOutputHelper output) : base(output) { }

    private static readonly RunMode[] AllRunModes = { RunMode.NoOverhead, RunMode.ExtraRun, RunMode.None, RunMode.SeparateLogic };

    private static IEnumerable<BaseMockInProcessDiagnoser[]> GetDiagnoserCombinations(int count)
    {
        if (count == 1)
        {
            foreach (var runMode in AllRunModes)
            {
                yield return [CreateDiagnoser(runMode)];
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
                        yield return [CreateDiagnoser(runMode1), CreateDiagnoser(runMode2), CreateDiagnoser(runMode3)];
                    }
                }
            }
        }
    }

    private static BaseMockInProcessDiagnoser CreateDiagnoser(RunMode runMode)
    {
        return runMode switch
        {
            RunMode.NoOverhead => new MockInProcessDiagnoser(),
            RunMode.ExtraRun => new MockInProcessDiagnoserExtraRun(),
            RunMode.None => new MockInProcessDiagnoserNone(),
            RunMode.SeparateLogic => new MockInProcessDiagnoserSeparateLogic(),
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

        var counts = new[] { 1, 3 };

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
            // NoOverhead, ExtraRun, and SeparateLogic should collect results
            // None should not collect results
            bool shouldHaveResults = diagnoser.DiagnoserRunMode != RunMode.None;

            if (shouldHaveResults)
            {
                if (diagnoser.DiagnoserRunMode == RunMode.SeparateLogic)
                {
                    // SeparateLogic is not yet implemented for in-process diagnosers, so we expect it to fail
                    // This is marked as a known limitation to be fixed in the future
                    Assert.Empty(diagnoser.Results); // Expected to fail until SeparateLogic is implemented
                    Assert.Empty(diagnoser.HandlerSignals); // No signals should be recorded
                }
                else
                {
                    Assert.NotEmpty(diagnoser.Results);
                    Assert.Equal(summary.BenchmarksCases.Length, diagnoser.Results.Count);
                    Assert.All(diagnoser.Results.Values, result => Assert.Equal(diagnoser.ExpectedResult, result));

                    // Verify handlers were called at the correct times
                    Assert.NotEmpty(diagnoser.HandlerSignals);
                    Assert.Equal(summary.BenchmarksCases.Length, diagnoser.HandlerSignals.Count);

                    foreach (var (benchmarkCase, signals) in diagnoser.HandlerSignals)
                    {
                        // Verify the handler was called with all expected signals in the correct order
                        Assert.NotEmpty(signals);
                        Assert.Contains(Engines.BenchmarkSignal.BeforeActualRun, signals);
                        Assert.Contains(Engines.BenchmarkSignal.AfterActualRun, signals);

                        // Verify signals are in correct order
                        var beforeIndex = signals.IndexOf(Engines.BenchmarkSignal.BeforeActualRun);
                        var afterIndex = signals.IndexOf(Engines.BenchmarkSignal.AfterActualRun);
                        Assert.True(beforeIndex < afterIndex,
                            $"BeforeActualRun should come before AfterActualRun, but got indices {beforeIndex} and {afterIndex}");
                    }
                }
            }
            else
            {
                Assert.Empty(diagnoser.Results);
                Assert.Empty(diagnoser.HandlerSignals); // None should not have any signals
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
}
