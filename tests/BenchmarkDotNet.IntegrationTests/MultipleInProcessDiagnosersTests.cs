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

    [Fact]
    public void MultipleInProcessDiagnosersWithNoOverheadRunMode()
    {
        var logger = new OutputLogger(Output);
        var diagnoser1 = new MockInProcessDiagnoserNoOverhead();
        var diagnoser2 = new MockInProcessDiagnoser();

        var config = CreateInProcessConfig(logger)
            .AddDiagnoser(diagnoser1)
            .AddDiagnoser(diagnoser2);

        var summary = CanExecute<SimpleBenchmark>(config);

        // Both diagnosers should have results for each benchmark
        Assert.NotEmpty(diagnoser1.Results);
        Assert.NotEmpty(diagnoser2.Results);
        Assert.Equal(summary.BenchmarksCases.Length, diagnoser1.Results.Count);
        Assert.Equal(summary.BenchmarksCases.Length, diagnoser2.Results.Count);

        // Verify the results are correct for each diagnoser
        Assert.All(diagnoser1.Results.Values, result => Assert.Equal("NoOverheadResult", result));
        Assert.All(diagnoser2.Results.Values, result => Assert.Equal("MockResult", result));
    }

    [Fact]
    public void MultipleInProcessDiagnosersWithExtraRunRunMode()
    {
        var logger = new OutputLogger(Output);
        var diagnoser1 = new MockInProcessDiagnoserExtraRun();
        var diagnoser2 = new MockInProcessDiagnoserNoOverhead();

        var config = CreateInProcessConfig(logger)
            .AddDiagnoser(diagnoser1)
            .AddDiagnoser(diagnoser2);

        var summary = CanExecute<SimpleBenchmark>(config);

        // Both diagnosers should have results
        Assert.NotEmpty(diagnoser1.Results);
        Assert.NotEmpty(diagnoser2.Results);
        Assert.Equal(summary.BenchmarksCases.Length, diagnoser1.Results.Count);
        Assert.Equal(summary.BenchmarksCases.Length, diagnoser2.Results.Count);

        // Verify the results are correct for each diagnoser
        Assert.All(diagnoser1.Results.Values, result => Assert.Equal("ExtraRunResult", result));
        Assert.All(diagnoser2.Results.Values, result => Assert.Equal("NoOverheadResult", result));
    }

    [Fact]
    public void MultipleInProcessDiagnosersWithVaryingRunModes()
    {
        var logger = new OutputLogger(Output);
        var noOverheadDiagnoser = new MockInProcessDiagnoserNoOverhead();
        var extraRunDiagnoser = new MockInProcessDiagnoserExtraRun();
        var noneDiagnoser = new MockInProcessDiagnoserNone();

        var config = CreateInProcessConfig(logger)
            .AddDiagnoser(noOverheadDiagnoser)
            .AddDiagnoser(extraRunDiagnoser)
            .AddDiagnoser(noneDiagnoser);

        var summary = CanExecute<SimpleBenchmark>(config);

        // NoOverhead and ExtraRun diagnosers should have results
        Assert.NotEmpty(noOverheadDiagnoser.Results);
        Assert.NotEmpty(extraRunDiagnoser.Results);
        Assert.Equal(summary.BenchmarksCases.Length, noOverheadDiagnoser.Results.Count);
        Assert.Equal(summary.BenchmarksCases.Length, extraRunDiagnoser.Results.Count);

        // None diagnoser should not have results (RunMode.None means it shouldn't run)
        Assert.Empty(noneDiagnoser.Results);

        // Verify the results are correct for diagnosers that ran
        Assert.All(noOverheadDiagnoser.Results.Values, result => Assert.Equal("NoOverheadResult", result));
        Assert.All(extraRunDiagnoser.Results.Values, result => Assert.Equal("ExtraRunResult", result));
    }

    [Fact]
    public void ThreeDifferentTypesOfInProcessDiagnosers()
    {
        var logger = new OutputLogger(Output);
        var noOverheadDiagnoser = new MockInProcessDiagnoserNoOverhead();
        var mockDiagnoser = new MockInProcessDiagnoser();
        var extraRunDiagnoser = new MockInProcessDiagnoserExtraRun();

        var config = CreateInProcessConfig(logger)
            .AddDiagnoser(noOverheadDiagnoser)
            .AddDiagnoser(mockDiagnoser)
            .AddDiagnoser(extraRunDiagnoser);

        var summary = CanExecute<SimpleBenchmark>(config);

        // All three diagnosers should have results
        Assert.NotEmpty(noOverheadDiagnoser.Results);
        Assert.NotEmpty(mockDiagnoser.Results);
        Assert.NotEmpty(extraRunDiagnoser.Results);
        Assert.Equal(summary.BenchmarksCases.Length, noOverheadDiagnoser.Results.Count);
        Assert.Equal(summary.BenchmarksCases.Length, mockDiagnoser.Results.Count);
        Assert.Equal(summary.BenchmarksCases.Length, extraRunDiagnoser.Results.Count);

        // Verify the results are correct for each diagnoser
        Assert.All(noOverheadDiagnoser.Results.Values, result => Assert.Equal("NoOverheadResult", result));
        Assert.All(mockDiagnoser.Results.Values, result => Assert.Equal("MockResult", result));
        Assert.All(extraRunDiagnoser.Results.Values, result => Assert.Equal("ExtraRunResult", result));
    }

    [Fact]
    public void MultipleInProcessDiagnosersWithMultipleBenchmarks()
    {
        var logger = new OutputLogger(Output);
        var diagnoser1 = new MockInProcessDiagnoserNoOverhead();
        var diagnoser2 = new MockInProcessDiagnoserExtraRun();

        var config = CreateInProcessConfig(logger)
            .AddDiagnoser(diagnoser1)
            .AddDiagnoser(diagnoser2);

        var summary = CanExecute<MultipleBenchmarks>(config);

        // Both diagnosers should have results for all benchmarks
        Assert.NotEmpty(diagnoser1.Results);
        Assert.NotEmpty(diagnoser2.Results);
        Assert.Equal(summary.BenchmarksCases.Length, diagnoser1.Results.Count);
        Assert.Equal(summary.BenchmarksCases.Length, diagnoser2.Results.Count);

        // Verify each diagnoser has a result for each benchmark method
        var benchmarkMethods = summary.BenchmarksCases.Select(bc => bc.Descriptor.WorkloadMethod.Name).ToList();
        Assert.Contains("Benchmark1", benchmarkMethods);
        Assert.Contains("Benchmark2", benchmarkMethods);
        Assert.Contains("Benchmark3", benchmarkMethods);
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
