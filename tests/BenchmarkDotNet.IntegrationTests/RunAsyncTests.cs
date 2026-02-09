using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class RunAsyncTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static TheoryData<IToolchain> GetInProcessToolchains() => new(
    [
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = false }),
    ]);

    private void ExecuteAndAssert<TBenchmark>(IToolchain toolchain, bool expectsAsync)
    {
        using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        var valueTask = CanExecuteAsync<TBenchmark>(config);
        Assert.NotEqual(expectsAsync, valueTask.IsCompleted);
        context.ExecuteUntilComplete(valueTask);
        Assert.True(valueTask.IsCompletedSuccessfully);
    }

    [Fact]
    public void OutOfProcessBenchmarks()
        => ExecuteAndAssert<BenchmarkAsync>(Job.Default.GetToolchain(), true);

    [Theory]
    [MemberData(nameof(GetInProcessToolchains), DisableDiscoveryEnumeration = true)]
    public void InProcessSyncBenchmarksRunSync(IToolchain toolchain)
        => ExecuteAndAssert<BenchmarkSync>(toolchain, false);

    [Theory]
    [MemberData(nameof(GetInProcessToolchains), DisableDiscoveryEnumeration = true)]
    public void InProcessAsyncBenchmarksRunAsync(IToolchain toolchain)
        => ExecuteAndAssert<BenchmarkAsync>(toolchain, true);

    public class BenchmarkSync
    {
        [GlobalSetup] public void GlobalSetup() { }
        [GlobalCleanup] public void GlobalCleanup() { }
        [IterationSetup] public void IterationSetup() { }
        [IterationCleanup] public void IterationCleanup() { }

        [Benchmark] public void ReturnVoid() { }
        [Benchmark] public object ReturnObject() => new();
    }

    public class BenchmarkAsync
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