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
                    // Default toolchain is much slower than in-process toolchains, so to prevent CI from taking too much time, we skip combinations with duplicate run modes.
                    if (toolchain == ToolchainType.Default && runModes.Length == 3
                        && (runModes[0] == runModes[1] || runModes[0] == runModes[2] || runModes[1] == runModes[2]))
                    {
                        continue;
                    }
                    yield return [runModes, toolchain];
                }
            }
        }
    }

    private static BaseMockInProcessDiagnoser CreateDiagnoser(RunMode runMode, int index)
        => index switch
        {
            0 => new MockInProcessDiagnoser1(runMode),
            1 => new MockInProcessDiagnoser2(runMode),
            2 => new MockInProcessDiagnoser3(runMode),
            _ => throw new ArgumentException($"Unsupported index: {index}")
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
        try
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
                if (diagnoser.RunMode == RunMode.None)
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
                diagnosers
                    .Where(d => d.RunMode != RunMode.None)
                    .OrderBy(d => d.RunMode switch
                    {
                        RunMode.NoOverhead => 0,
                        RunMode.ExtraIteration => 1,
                        RunMode.ExtraRun => 2,
                        RunMode.SeparateLogic => 3,
                        _ => 4
                    })
                    .Select(d => d.ExpectedResult),
                GetOrderedResults()
            );
        }
        finally
        {
            BaseMockInProcessDiagnoser.s_completedResults.Clear();
        }
    }

    private static IEnumerable<string> GetOrderedResults()
    {
        // RunMode.NoOverhead and RunMode.ExtraIteration run in the same benchmark run,
        // so we need to re-order the completed results according to the order that they received their signal,
        // while leaving the other run modes in the same spot.
        var unorderedResults = new Queue<(int order, string result)>();

        bool wasLastOrdered = true;
        foreach (var (runMode, order, result) in BaseMockInProcessDiagnoser.s_completedResults)
        {
            if (runMode is RunMode.NoOverhead or RunMode.ExtraIteration)
            {
                wasLastOrdered = false;
                unorderedResults.Enqueue((order, result));
                continue;
            }

            if (!wasLastOrdered)
            {
                wasLastOrdered = true;
                foreach (var (_, r) in unorderedResults.OrderBy(t => t.order))
                {
                    yield return r;
                }
                unorderedResults.Clear();
            }
            yield return result;
        }

        foreach (var (_, result) in unorderedResults.OrderBy(t => t.order))
        {
            yield return result;
        }
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
