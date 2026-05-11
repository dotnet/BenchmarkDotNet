using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Runtime.CompilerServices;

// BDN1701 is the analyzer that mirrors AwaitableAsyncEnumerableAmbiguityValidator. The test types in this
// file intentionally exhibit the pattern this validator warns about, so suppress the analyzer here — the
// runtime validator is what we are testing.
#pragma warning disable BDN1701

namespace BenchmarkDotNet.Tests.Validators;

public class AwaitableAsyncEnumerableAmbiguityValidatorTests
{
    // A struct that satisfies BOTH the awaitable pattern (parameterless GetAwaiter returning an awaiter
    // with IsCompleted/GetResult/OnCompleted) AND the async-enumerable pattern (public GetAsyncEnumerator
    // returning an enumerator with MoveNextAsync awaitable-to-bool and a Current property).
    public readonly struct DualShaped
    {
        public DualShapedAwaiter GetAwaiter() => default;
        public DualShapedEnumerator GetAsyncEnumerator() => default;
    }

    public readonly struct DualShapedAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public int GetResult() => 0;
        public void OnCompleted(Action continuation) => continuation();
    }

    public struct DualShapedEnumerator
    {
        public int Current => 0;
        public DualShapedAwaitableBool MoveNextAsync() => default;
    }

    public readonly struct DualShapedAwaitableBool
    {
        public DualShapedBoolAwaiter GetAwaiter() => default;
    }

    public readonly struct DualShapedBoolAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public bool GetResult() => false;
        public void OnCompleted(Action continuation) => continuation();
    }

    public class DualShapedBenchmarkClass
    {
        [Benchmark]
        public DualShaped Run() => default;
    }

    [Fact]
    public async Task DualShapedBenchmarkReturnTypeIsWarned()
    {
        var validationErrors = await AwaitableAsyncEnumerableAmbiguityValidator.DontFailOnError.ValidateAsync(
            BenchmarkConverter.TypeToBenchmarks(typeof(DualShapedBenchmarkClass))).ToArrayAsync();

        Assert.Contains(validationErrors, v =>
            !v.IsCritical
            && v.Message.Contains("[BenchmarkAttribute]")
            && v.Message.Contains("Run")
            && v.Message.Contains("awaitable that also matches the async enumerable pattern"));
    }

    public class DualShapedGlobalSetupClass
    {
        [GlobalSetup]
        public DualShaped Setup() => default;

        [Benchmark]
        public void Benchmark() { }
    }

    [Fact]
    public async Task DualShapedGlobalSetupReturnTypeIsWarned()
    {
        var validationErrors = await AwaitableAsyncEnumerableAmbiguityValidator.DontFailOnError.ValidateAsync(
            BenchmarkConverter.TypeToBenchmarks(typeof(DualShapedGlobalSetupClass))).ToArrayAsync();

        Assert.Contains(validationErrors, v =>
            !v.IsCritical
            && v.Message.Contains("[GlobalSetupAttribute]")
            && v.Message.Contains("Setup")
            && v.Message.Contains("awaitable that also matches the async enumerable pattern"));
    }

    public class DualShapedIterationCleanupClass
    {
        [IterationCleanup]
        public DualShaped Cleanup() => default;

        [Benchmark]
        public void Benchmark() { }
    }

    [Fact]
    public async Task DualShapedIterationCleanupReturnTypeIsWarned()
    {
        var validationErrors = await AwaitableAsyncEnumerableAmbiguityValidator.DontFailOnError.ValidateAsync(
            BenchmarkConverter.TypeToBenchmarks(typeof(DualShapedIterationCleanupClass))).ToArrayAsync();

        Assert.Contains(validationErrors, v =>
            !v.IsCritical
            && v.Message.Contains("[IterationCleanupAttribute]")
            && v.Message.Contains("Cleanup")
            && v.Message.Contains("awaitable that also matches the async enumerable pattern"));
    }

    public class PureAwaitableBenchmarkClass
    {
        [Benchmark]
        public async Task Run() => await Task.Yield();
    }

    [Fact]
    public async Task PureAwaitableBenchmarkProducesNoWarning()
    {
        var validationErrors = await AwaitableAsyncEnumerableAmbiguityValidator.DontFailOnError.ValidateAsync(
            BenchmarkConverter.TypeToBenchmarks(typeof(PureAwaitableBenchmarkClass))).ToArrayAsync();

        Assert.Empty(validationErrors);
    }

    public class PureAsyncEnumerableBenchmarkClass
    {
        [Benchmark]
        public async IAsyncEnumerable<int> Producer()
        {
            await Task.Yield();
            yield return 1;
        }
    }

    [Fact]
    public async Task PureAsyncEnumerableBenchmarkProducesNoWarning()
    {
        var validationErrors = await AwaitableAsyncEnumerableAmbiguityValidator.DontFailOnError.ValidateAsync(
            BenchmarkConverter.TypeToBenchmarks(typeof(PureAsyncEnumerableBenchmarkClass))).ToArrayAsync();

        Assert.Empty(validationErrors);
    }
}
