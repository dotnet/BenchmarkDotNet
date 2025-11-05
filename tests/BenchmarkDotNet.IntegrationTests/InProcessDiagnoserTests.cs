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

public class InProcessDiagnoserTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    // For test explorer since it doesn't handle interfaces well.
    public enum ToolchainType
    {
        Default,
        InProcessEmit,
        InProcessNoEmit,
    }

    private static IEnumerable<RunMode[]> GetRunModeCombinations(int count)
    {
        var runModes = (RunMode[]) Enum.GetValues(typeof(RunMode));

        if (count == 1)
        {
            foreach (var runMode in runModes)
            {
                yield return [runMode];
            }
        }
        else if (count == 3)
        {
            foreach (var runMode1 in runModes)
            {
                foreach (var runMode2 in runModes)
                {
                    foreach (var runMode3 in runModes)
                    {
                        yield return [runMode1, runMode2, runMode3];
                    }
                }
            }
        }
    }

    public static IEnumerable<object[]> GetTestCombinations()
    {
        var toolchains = (ToolchainType[]) Enum.GetValues(typeof(ToolchainType));
        var counts = new[] { 1, 3 };

        foreach (var toolchain in toolchains)
        {
            foreach (var count in counts)
            {
                foreach (var runModes in GetRunModeCombinations(count))
                {
                    yield return [runModes, toolchain];
                }
            }
        }
    }

    private static BaseMockInProcessDiagnoser CreateDiagnoser(RunMode runMode)
        => runMode switch
        {
            RunMode.None => new MockInProcessDiagnoserNone(),
            RunMode.NoOverhead => new MockInProcessDiagnoser(),
            RunMode.ExtraRun => new MockInProcessDiagnoserExtraRun(),
            RunMode.SeparateLogic => new MockInProcessDiagnoserSeparateLogic(),
            _ => throw new ArgumentException($"Unsupported run mode: {runMode}")
        };

    private ManualConfig CreateConfig(ToolchainType toolchain)
    {
        var job = toolchain switch
        {
            ToolchainType.InProcessEmit => Job.Dry.WithToolchain(InProcessEmitToolchain.Instance),
            ToolchainType.InProcessNoEmit => Job.Dry.WithToolchain(InProcessNoEmitToolchain.Instance),
            _ => Job.Dry
        };

        return new ManualConfig()
            .AddLogger(new OutputLogger(Output))
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddJob(job);
    }

    [Theory]
    [MemberData(nameof(GetTestCombinations))]
    public void MultipleInProcessDiagnosersWork(RunMode[] runModes, ToolchainType toolchain)
    {
        var diagnosers = runModes.Select(CreateDiagnoser).ToArray();
        var config = CreateConfig(toolchain);

        foreach (var diagnoser in diagnosers)
        {
            config = config.AddDiagnoser(diagnoser);
        }

        var summary = CanExecute<SimpleBenchmark>(config);

        foreach (var diagnoser in diagnosers)
        {
            if (diagnoser.DiagnoserRunMode == RunMode.None)
            {
                Assert.Empty(diagnoser.Results);
            }
            else
            {
                Assert.NotEmpty(diagnoser.Results);
                Assert.Equal(summary.BenchmarksCases.Length, diagnoser.Results.Count);
                Assert.All(diagnoser.Results.Values, result => Assert.Equal(diagnoser.ExpectedResult, result));
            }
        }
        Assert.Equal(
            BaseMockInProcessDiagnoser.s_completedResults,
            diagnosers
                .Where(d => d.DiagnoserRunMode != RunMode.None)
                .OrderBy(d => d.DiagnoserRunMode switch
                {
                    RunMode.NoOverhead => 0,
                    RunMode.ExtraRun => 1,
                    RunMode.SeparateLogic => 2,
                    _ => 3
                })
        );
        BaseMockInProcessDiagnoser.s_completedResults.Clear();
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
