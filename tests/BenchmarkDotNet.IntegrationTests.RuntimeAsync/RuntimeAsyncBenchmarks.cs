using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.IntegrationTests.RuntimeAsync;

public class RuntimeAsyncTaskCaseBenchmark
{
    [GlobalSetup]
    public async ValueTask GlobalSetup() => await Task.Yield();

    [GlobalCleanup]
    public async Task GlobalCleanup() => await Task.Yield();

    [IterationSetup]
    public async ValueTask IterationSetup() => await Task.Yield();

    [IterationCleanup]
    public async Task IterationCleanup() => await Task.Yield();

    [Benchmark]
    public async Task TaskCase1() => await Task.CompletedTask;

    [Benchmark, Arguments(1, "1", 0.1)]
    public async Task TaskCase1(int x, string y, double? z) => await Task.CompletedTask;

    [Benchmark]
    public async Task<string> TaskCase2() => await Task.FromResult("123");

    [Benchmark, Arguments(2, "2", 0.2)]
    public async Task<string> TaskCase2(int x, string y, double? z) => await Task.FromResult("123");

    [Benchmark]
    public async ValueTask TaskCase3() => await Task.CompletedTask;

    [Benchmark, Arguments(3, "3", 0.3)]
    public async ValueTask TaskCase3(int x, string y, double? z) => await Task.CompletedTask;

    [Benchmark]
    public async ValueTask<bool> TaskCase4() => await new ValueTask<bool>(true);

    [Benchmark, Arguments(4, "4", 0.4)]
    public async ValueTask<bool> TaskCase4(int x, string y, double? z) => await new ValueTask<bool>(true);
}

// The generated wrappers follow each setup and cleanup method, whatever the workload is.
public class RuntimeAsyncSetupCleanupBenchmark
{
    [GlobalSetup]
    public async Task GlobalSetup() => await Task.Yield();

    [GlobalCleanup]
    public static async ValueTask GlobalCleanup() => await Task.Yield();

    [IterationSetup]
    public async Task<int> IterationSetup() => await Task.FromResult(1);

    [IterationCleanup]
    public async ValueTask<int> IterationCleanup() => await new ValueTask<int>(1);

    [Benchmark]
    public void Workload() { }
}

// Roslyn ignores a builder override on a runtime-async method but keeps the attribute, which the generated WorkloadCore copies.
public class RuntimeAsyncBuilderOverrideBenchmark
{
    [Benchmark]
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public async ValueTask Pooled() => await Task.CompletedTask;

    [Benchmark]
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<int>))]
    public async ValueTask<int> PooledResult() => await new ValueTask<int>(1);
}
