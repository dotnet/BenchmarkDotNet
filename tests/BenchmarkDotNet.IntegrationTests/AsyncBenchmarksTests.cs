using BenchmarkDotNet.Attributes;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.IntegrationTests
{
    internal class ValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core;

        T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);
        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => _core.GetStatus(token);
        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
        void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        public void Reset() => _core.Reset();
        public short Token => _core.Version;
        public void SetResult(T result) => _core.SetResult(result);
    }

    // This is used to test the case of ValueTaskAwaiter.IsCompleted returns false, then OnCompleted invokes the callback immediately because it happened to complete between the 2 calls.
    internal class ValueTaskSourceCallbackOnly<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core;

        T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);
        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
        // Always return pending state so OnCompleted will be called.
        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => ValueTaskSourceStatus.Pending;
        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => ValueTaskSourceStatus.Pending;
        void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        public void Reset() => _core.Reset();
        public short Token => _core.Version;
        public void SetResult(T result) => _core.SetResult(result);
    }

    public class AsyncBenchmarksTests : BenchmarkTestExecutor
    {
        public AsyncBenchmarksTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<IToolchain> GetToolchains() =>
        [
            new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = false }),
            new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = true }),
            new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = false }),
            new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = true }),
            Job.Default.GetToolchain()
        ];

        public static TheoryData<IToolchain, bool> GetToolchainsWithConsumeTasksSynchronously()
        {
            var data = new TheoryData<IToolchain, bool>();
            foreach (var toolchain in GetToolchains())
            {
                data.Add(toolchain, false);
                data.Add(toolchain, true);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(GetToolchainsWithConsumeTasksSynchronously), DisableDiscoveryEnumeration = true)]
        public void TaskReturningMethodsAreAwaited(IToolchain toolchain, bool consumeTasksSynchronously)
        {
            var summary = CanExecute<TaskDelayMethods>(CreateSimpleConfig(
                job: Job.Dry.WithToolchain(toolchain).WithConsumeTasksSynchronously(consumeTasksSynchronously)));

            foreach (var report in summary.Reports)
                foreach (var measurement in report.AllMeasurements)
                {
                    double actual = measurement.Nanoseconds;
                    const double minExpected = TaskDelayMethods.NanosecondsDelay - TaskDelayMethods.MaxTaskDelayResolutionInNanoseconds;
                    string name = report.BenchmarkCase.Descriptor.GetFilterName();
                    Assert.True(actual > minExpected, $"{name} has not been awaited, took {actual}ns, while it should take more than {minExpected}ns");
                }
        }

        [Theory]
        [MemberData(nameof(GetToolchainsWithConsumeTasksSynchronously), DisableDiscoveryEnumeration = true)]
        public void TaskReturningMethodsAreAwaited_AlreadyComplete(IToolchain toolchain, bool consumeTasksSynchronously)
            => CanExecute<TaskImmediateMethods>(CreateSimpleConfig(
                job: Job.Dry.WithToolchain(toolchain).WithConsumeTasksSynchronously(consumeTasksSynchronously)));

        [Theory]
        [MemberData(nameof(GetToolchainsWithConsumeTasksSynchronously), DisableDiscoveryEnumeration = true)]
        public void TaskYieldWithNullSyncContext(IToolchain toolchain, bool consumeTasksSynchronously)
            => CanExecute<NullSyncContextBenchmarks>(CreateSimpleConfig(
                job: Job.Dry.WithToolchain(toolchain).WithConsumeTasksSynchronously(consumeTasksSynchronously)));

        // #3103
        [Theory]
        [MemberData(nameof(GetToolchainsWithConsumeTasksSynchronously), DisableDiscoveryEnumeration = true)]
        public void AsyncWorkloadRestartsAfterMemoryRandomization(IToolchain toolchain, bool consumeTasksSynchronously)
            => CanExecute<RandomMemoryAsyncBenchmarks>(CreateSimpleConfig(
                job: Job.Dry.WithToolchain(toolchain).WithIterationCount(3).WithMemoryRandomization(true).WithConsumeTasksSynchronously(consumeTasksSynchronously)));

        public class RandomMemoryAsyncBenchmarks
        {
            [GlobalSetup(Targets = new[] { nameof(ReturningTask), nameof(ReturningGenericTask), nameof(AwaitingValueTask) })]
            public void GlobalSetup() { }

            [GlobalCleanup(Targets = new[] { nameof(ReturningGenericTask), nameof(AwaitingTask) })]
            public void GlobalCleanup() { }

            [Benchmark] public Task ReturningTask() => Task.CompletedTask;
            [Benchmark] public ValueTask ReturningValueTask() => default;
            [Benchmark] public Task<int> ReturningGenericTask() => Task.FromResult(0);
            [Benchmark] public ValueTask<int> ReturningGenericValueTask() => new(0);
            [Benchmark] public async Task AwaitingTask() => await Task.Yield();
            [Benchmark] public async ValueTask AwaitingValueTask() => await Task.Yield();
        }

        public class TaskDelayMethods
        {
            private readonly ValueTaskSource<int> valueTaskSource = new();

            private const int MillisecondsDelay = 100;

            internal const double NanosecondsDelay = MillisecondsDelay * 1e+6;

            // The default frequency of the Windows System Timer is 64Hz, so the Task.Delay error is up to 15.625ms.
            internal const int MaxTaskDelayResolutionInNanoseconds = 1_000_000_000 / 64;

            [Benchmark]
            public Task ReturningTask() => Task.Delay(MillisecondsDelay);

            [Benchmark]
            public ValueTask ReturningValueTask() => new ValueTask(Task.Delay(MillisecondsDelay));

            [Benchmark]
            public ValueTask ReturningValueTaskBackByIValueTaskSource()
            {
                valueTaskSource.Reset();
                Task.Delay(MillisecondsDelay).ContinueWith(_ =>
                {
                    valueTaskSource.SetResult(default);
                });
                return new ValueTask(valueTaskSource, valueTaskSource.Token);
            }

            [Benchmark]
            public async Task Awaiting() => await Task.Delay(MillisecondsDelay);

            [Benchmark]
            public Task<int> ReturningGenericTask() => ReturningTask().ContinueWith(_ => default(int));

            [Benchmark]
            public ValueTask<int> ReturningGenericValueTask() => new ValueTask<int>(ReturningGenericTask());

            [Benchmark]
            public ValueTask<int> ReturningGenericValueTaskBackByIValueTaskSource()
            {
                valueTaskSource.Reset();
                Task.Delay(MillisecondsDelay).ContinueWith(_ =>
                {
                    valueTaskSource.SetResult(default);
                });
                return new ValueTask<int>(valueTaskSource, valueTaskSource.Token);
            }
        }

        public class NullSyncContextBenchmarks
        {
            [GlobalSetup]
            public void Setup() => SynchronizationContext.SetSynchronizationContext(null);

            [Benchmark]
            public async Task TaskYield() => await Task.Yield();
        }

        public class TaskImmediateMethods
        {
            private readonly ValueTaskSource<int> valueTaskSource = new();
            private readonly ValueTaskSourceCallbackOnly<int> valueTaskSourceCallbackOnly = new();

            [Benchmark]
            public Task ReturningTask() => Task.CompletedTask;

            [Benchmark]
            public ValueTask ReturningValueTask() => new ValueTask();

            [Benchmark]
            public ValueTask ReturningValueTaskBackByIValueTaskSource()
            {
                valueTaskSource.Reset();
                valueTaskSource.SetResult(default);
                return new ValueTask(valueTaskSource, valueTaskSource.Token);
            }

            [Benchmark]
            public ValueTask ReturningValueTaskBackByIValueTaskSource_ImmediateCallback()
            {
                valueTaskSourceCallbackOnly.Reset();
                valueTaskSourceCallbackOnly.SetResult(default);
                return new ValueTask(valueTaskSourceCallbackOnly, valueTaskSourceCallbackOnly.Token);
            }

            [Benchmark]
            public async Task Awaiting() => await Task.CompletedTask;

            [Benchmark]
            public Task<int> ReturningGenericTask() => ReturningTask().ContinueWith(_ => default(int));

            [Benchmark]
            public ValueTask<int> ReturningGenericValueTask() => new ValueTask<int>(ReturningGenericTask());

            [Benchmark]
            public ValueTask<int> ReturningGenericValueTaskBackByIValueTaskSource()
            {
                valueTaskSource.Reset();
                valueTaskSource.SetResult(default);
                return new ValueTask<int>(valueTaskSource, valueTaskSource.Token);
            }

            [Benchmark]
            public ValueTask<int> ReturningGenericValueTaskBackByIValueTaskSource_ImmediateCallback()
            {
                valueTaskSourceCallbackOnly.Reset();
                valueTaskSourceCallbackOnly.SetResult(default);
                return new ValueTask<int>(valueTaskSourceCallbackOnly, valueTaskSourceCallbackOnly.Token);
            }
        }
    }
}