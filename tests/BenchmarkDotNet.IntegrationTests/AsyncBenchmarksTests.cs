using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    internal class ValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core;

        T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);
        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => _core.GetStatus(token);
        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
        void IValueTaskSource<T>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
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
        void IValueTaskSource<T>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        public void Reset() => _core.Reset();
        public short Token => _core.Version;
        public void SetResult(T result) => _core.SetResult(result);
    }

    public class AsyncBenchmarksTests : BenchmarkTestExecutor
    {
        public AsyncBenchmarksTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TaskReturningMethodsAreAwaited()
        {
            var summary = CanExecute<TaskDelayMethods>();

            foreach (var report in summary.Reports)
            foreach (var measurement in report.AllMeasurements)
            {
                double actual = measurement.Nanoseconds;
                const double minExpected = TaskDelayMethods.NanosecondsDelay - TaskDelayMethods.MaxTaskDelayResolutionInNanoseconds;
                string name = report.BenchmarkCase.Descriptor.GetFilterName();
                Assert.True(actual > minExpected, $"{name} has not been awaited, took {actual}ns, while it should take more than {minExpected}ns");
            }
        }

        [Fact]
        public void TaskReturningMethodsAreAwaited_AlreadyComplete() => CanExecute<TaskImmediateMethods>();

        public class TaskDelayMethods
        {
            private readonly ValueTaskSource<int> valueTaskSource = new ();

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

        public class TaskImmediateMethods
        {
            private readonly ValueTaskSource<int> valueTaskSource = new ();
            private readonly ValueTaskSourceCallbackOnly<int> valueTaskSourceCallbackOnly = new ();

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