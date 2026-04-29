using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.IntegrationTests;

public class RunAsyncTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static TheoryData<IToolchain> GetToolchains() =>
    [
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = true }),
        new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = true }),
        Job.Default.GetToolchain()
    ];

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void AsyncBenchmarksRunAsync(IToolchain toolchain)
    {
        using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        var valueTask = CanExecuteAsync<AsyncBenchmarks>(config);
        Assert.False(valueTask.IsCompleted);
        context.ExecuteUntilComplete(valueTask);
        Assert.True(valueTask.IsCompletedSuccessfully);
    }

    public class AsyncBenchmarks
    {
        [GlobalSetup] public async Task GlobalSetup() => await Task.Yield();

        [GlobalCleanup] public async Task GlobalCleanup() => await Task.Yield();
        [IterationSetup] public async Task IterationSetup() => await Task.Yield();
        [IterationCleanup] public async Task IterationCleanup() => await Task.Yield();

        [Benchmark] public async Task AsyncTask() => await Task.Yield();

        [Benchmark]
        public async ValueTask<object> AsyncValueTaskObject()
        {
            await Task.Yield();
            return new();
        }
    }
}