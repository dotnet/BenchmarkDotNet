using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.IntegrationTests;

public class AsyncEnumerableBenchmarksTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static TheoryData<IToolchain> GetAllToolchains() =>
    [
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = true }),
        new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = true }),
        Job.Default.GetToolchain()
    ];

    // InProcessNoEmitToolchain does not support custom async enumerables or [AsyncCallerType].
    public static TheoryData<IToolchain> GetCustomSupportedToolchains() =>
    [
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = true }),
        Job.Default.GetToolchain()
    ];

    [Theory]
    [MemberData(nameof(GetAllToolchains), DisableDiscoveryEnumeration = true)]
    public void AsyncEnumerableMethodsAreConsumed(IToolchain toolchain)
        => CanExecute<AsyncEnumerableBenchmarks>(CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain)));

    [Theory]
    [MemberData(nameof(GetCustomSupportedToolchains), DisableDiscoveryEnumeration = true)]
    public void CustomAsyncEnumerableTypesAreConsumed(IToolchain toolchain)
    {
        CanExecute<CustomAsyncEnumerableBenchmarks>(CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain)));
    }

    [Theory]
    [MemberData(nameof(GetCustomSupportedToolchains), DisableDiscoveryEnumeration = true)]
    public void IAsyncEnumerableWithAsyncCallerTypeOverrideIsConsumed(IToolchain toolchain)
    {
        CanExecute<AsyncEnumerableCallerOverride>(CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain)));
    }

    [Fact]
    public void IteratorExceptionsAreReportedAsBenchmarkFailures()
    {
        // Throwing after the first yield exercises the consumer's MoveNextAsync loop: the first call
        // produces an item, the second call runs the rest of the body and throws. Surfacing this as
        // either a validation error (the validator drains too) or a benchmark execution failure is
        // acceptable — both prove the throw isn't silently swallowed.
        var summary = CanExecute<ThrowingIteratorBenchmark>(fullValidation: false);

        Assert.True(
            summary.HasCriticalValidationErrors
                || summary.Reports.Any(r => r.ExecuteResults?.Any(er => !er.IsSuccess) == true),
            "Expected the iterator throw to be surfaced as a validation error or benchmark execution failure.");
    }

    public class ThrowingIteratorBenchmark
    {
        [Benchmark]
        public async IAsyncEnumerable<int> ThrowsAfterFirstYield()
        {
            await Task.Yield();
            yield return 1;
            throw new InvalidOperationException("Iterator threw after first yield");
        }
    }

    public class AsyncEnumerableBenchmarks
    {
        [ParamsAllValues]
        public bool Yield { get; set; }

        private bool ranBody;

        [GlobalSetup]
        public void Setup() => ranBody = false;

        [GlobalCleanup]
        public void Verify()
        {
            // If the framework only invoked the iterator method without enumerating it, the body
            // never runs and this flag stays false — failing the benchmark and surfacing the bug.
            if (!ranBody)
                throw new InvalidOperationException("Async enumerable body was never executed; iterator was not consumed.");
        }

        [Benchmark]
        public async IAsyncEnumerable<int> Producer()
        {
            yield return 42;
            if (Yield)
            {
                await Task.Yield();
            }
            ranBody = true;
        }

        [Benchmark]
        public ConfiguredCancelableAsyncEnumerable<int> Configured()
            => Producer().WithCancellation(default);
    }

    public class CustomAsyncEnumerableBenchmarks
    {
        private bool ranBody;

        [GlobalSetup]
        public void Setup() => ranBody = false;

        [GlobalCleanup]
        public void Verify()
        {
            if (!ranBody)
                throw new InvalidOperationException("Custom enumerable's MoveNextAsync was never invoked; iterator was not consumed.");
        }

        [Benchmark]
        public CustomAsyncEnumerable<int> Custom()
            => new([1, 2, 3, 4], () => ranBody = true);
    }

    public readonly struct CustomAsyncEnumerable<T>(T[] items, Action onMoveNext)
    {
        public CustomAsyncEnumerator<T> GetAsyncEnumerator()
            => new(items, onMoveNext);
    }

    public struct CustomAsyncEnumerator<T>(T[] items, Action onMoveNext)
    {
        private int index = -1;

        public readonly T Current => items[index];

        public CustomTask<bool> MoveNextAsync()
        {
            onMoveNext();
            index++;
            return new CustomTask<bool>(new ValueTask<bool>(index < items.Length));
        }
    }

    public class AsyncEnumerableCallerOverride
    {
        [GlobalSetup]
        public void Setup() => Assert.Equal(0, AsyncCustomTaskMethodBuilder<bool>.InUseCounter);

        [GlobalCleanup]
        public void Verify() => Assert.Equal(0, AsyncCustomTaskMethodBuilder<bool>.InUseCounter);

        [Benchmark]
        [AsyncCallerType(typeof(CustomTask<bool>))]
        public async IAsyncEnumerable<int> Enumerate()
        {
            Assert.Equal(1, AsyncCustomTaskMethodBuilder<bool>.InUseCounter);
            await Task.Yield();
            yield return 1;
        }
    }
}
