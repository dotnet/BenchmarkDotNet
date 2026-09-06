using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.Tests.Validators
{
    public class ExecutionValidatorTests
    {
        [Fact]
        public async Task FailingConstructorsAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(FailingConstructor))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Unable to create instance of FailingConstructor", validationErrors.Single().Message);
            Assert.Contains("This one fails", validationErrors.Single().Message);
        }

        public class FailingConstructor
        {
            public FailingConstructor() => throw new Exception("This one fails");

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task FailingGlobalSetupsAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalSetup))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Failed to execute [GlobalSetup]", validationErrors.Single().Message);
            Assert.Contains("This one fails", validationErrors.Single().Message);
        }

        public class FailingGlobalSetup
        {
            [GlobalSetup]
            public void Failing() => throw new Exception("This one fails");

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task FailingGlobalCleanupsAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalCleanup))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Failed to execute [GlobalCleanup]", validationErrors.Single().Message);
            Assert.Contains("This one fails", validationErrors.Single().Message);
        }

        public class FailingGlobalCleanup
        {
            [GlobalCleanup]
            public void Failing() => throw new Exception("This one fails");

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task VirtualGlobalSetupsAreSupported()
        {
            Assert.False(OverridesGlobalSetup.WasCalled);
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(OverridesGlobalSetup))).ToArrayAsync();

            Assert.True(OverridesGlobalSetup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class BaseClassWithThrowingGlobalSetup
        {
            [GlobalSetup]
            public virtual void GlobalSetup() => throw new Exception("Should not be executed when overridden");

            [Benchmark]
            public void NonThrowing() { }
        }

        public class OverridesGlobalSetup : BaseClassWithThrowingGlobalSetup
        {
            public static bool WasCalled;

            [GlobalSetup]
            public override void GlobalSetup() => WasCalled = true;
        }

        [Fact]
        public async Task VirtualGlobalCleanupsAreSupported()
        {
            Assert.False(OverridesGlobalCleanup.WasCalled);
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(OverridesGlobalCleanup))).ToArrayAsync();

            Assert.True(OverridesGlobalCleanup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class BaseClassWithThrowingGlobalCleanup
        {
            [GlobalCleanup]
            public virtual void GlobalCleanup() => throw new Exception("Should not be executed when overridden");

            [Benchmark]
            public void NonThrowing() { }
        }

        public class OverridesGlobalCleanup : BaseClassWithThrowingGlobalCleanup
        {
            public static bool WasCalled;

            [GlobalCleanup]
            public override void GlobalCleanup() => WasCalled = true;
        }

        [Fact]
        public async Task NonFailingGlobalSetupsAreOmitted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(GlobalSetupThatRequiresParamsToBeSetFirst))).ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        public class GlobalSetupThatRequiresParamsToBeSetFirst
        {
            [Params(100)]
            [UsedImplicitly]
            public int Field;

            [GlobalSetup]
            public void Failing()
            {
                if (Field == default)
                    throw new Exception("This should have never happened");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        // #848
        [Fact]
        public async Task ParamsSourcePropertyIsSetBeforeGlobalSetup()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(GlobalSetupThatRequiresParamsSourceToBeSetFirst)))
                .ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        public class GlobalSetupThatRequiresParamsSourceToBeSetFirst
        {
            [ParamsSource(nameof(GetValues))]
            public int Field { get; set; }

            public static IEnumerable<int> GetValues() => [100];

            [GlobalSetup]
            public void Failing()
            {
                if (Field == default)
                    throw new Exception("ParamsSource property was not set before GlobalSetup");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task NonFailingGlobalCleanupsAreOmitted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(GlobalCleanupThatRequiresParamsToBeSetFirst))).ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        public class GlobalCleanupThatRequiresParamsToBeSetFirst
        {
            [Params(100)]
            [UsedImplicitly]
            public int Field;

            [GlobalCleanup]
            public void Failing()
            {
                if (Field == default)
                    throw new Exception("This should have never happened");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task MissingParamsAttributeThatMakesGlobalSetupsFailAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalSetupWhichShouldHaveHadParamsForField)))
                .ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Failed to execute [GlobalSetup]", validationErrors.Single().Message);
        }

        public class FailingGlobalSetupWhichShouldHaveHadParamsForField
        {
            [UsedImplicitly]
            public int Field;

            [GlobalSetup]
            public void Failing()
            {
                if (Field == default)
                    throw new Exception("Field is missing Params attribute");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task MissingParamsAttributeThatMakesGlobalCleanupsFailAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalCleanupWhichShouldHaveHadParamsForField)))
                .ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Failed to execute [GlobalCleanup]", validationErrors.Single().Message);
        }

        public class FailingGlobalCleanupWhichShouldHaveHadParamsForField
        {
            [UsedImplicitly]
            public int Field;

            [GlobalCleanup]
            public void Failing()
            {
                if (Field == default)
                    throw new Exception("Field is missing Params attribute");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void FieldsWithoutParamsValuesAreDiscovered()
        {
            Assert.Empty(BenchmarkConverter.TypeToBenchmarks(typeof(FieldsWithoutParamsValues)).BenchmarksCases);
        }

        public class FieldsWithoutParamsValues
        {
#pragma warning disable BDN1300
            [Params]
#pragma warning restore BDN1300
            [UsedImplicitly]
            public int FieldWithoutValuesSpecified;

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task NonFailingBenchmarksAreOmitted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(NonFailingBenchmark))).ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        public class NonFailingBenchmark
        {
            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task FailingBenchmarksAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(FailingBenchmark))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.Contains(validationErrors, error => error.Message.Contains("This benchmark throws"));
        }

        public class FailingBenchmark
        {
            [Benchmark]
            public void Throwing() => throw new Exception("This benchmark throws");
        }

        [Fact]
        public async Task MultipleParamsDoNotMultiplyGlobalSetup()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleParamsAndSingleGlobalSetup))).ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        public class MultipleParamsAndSingleGlobalSetup
        {
            [Params(1, 2)]
            [UsedImplicitly]
            public int Field;

            [GlobalSetup]
            public void Single() { }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncTaskGlobalSetupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncTaskGlobalSetup))).ToArrayAsync();

            Assert.True(AsyncTaskGlobalSetup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncTaskGlobalSetup
        {
            public static bool WasCalled;

            [GlobalSetup]
            public async Task GlobalSetup()
            {
                await Task.Delay(1);

                WasCalled = true;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncTaskGlobalCleanup))).ToArrayAsync();

            Assert.True(AsyncTaskGlobalCleanup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncTaskGlobalCleanup
        {
            public static bool WasCalled;

            [GlobalCleanup]
            public async Task GlobalCleanup()
            {
                await Task.Delay(1);

                WasCalled = true;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncGenericTaskGlobalSetupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericTaskGlobalSetup))).ToArrayAsync();

            Assert.True(AsyncGenericTaskGlobalSetup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncGenericTaskGlobalSetup
        {
            public static bool WasCalled;

            [GlobalSetup]
            public async Task<int> GlobalSetup()
            {
                await Task.Delay(1);

                WasCalled = true;

                return 42;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncGenericTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericTaskGlobalCleanup))).ToArrayAsync();

            Assert.True(AsyncGenericTaskGlobalCleanup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncGenericTaskGlobalCleanup
        {
            public static bool WasCalled;

            [GlobalCleanup]
            public async Task<int> GlobalCleanup()
            {
                await Task.Delay(1);

                WasCalled = true;

                return 42;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncValueTaskGlobalSetupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncValueTaskGlobalSetup))).ToArrayAsync();

            Assert.True(AsyncValueTaskGlobalSetup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncValueTaskGlobalSetup
        {
            public static bool WasCalled;

            [GlobalSetup]
            public async ValueTask GlobalSetup()
            {
                await Task.Delay(1);

                WasCalled = true;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncValueTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncValueTaskGlobalCleanup))).ToArrayAsync();

            Assert.True(AsyncValueTaskGlobalCleanup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncValueTaskGlobalCleanup
        {
            public static bool WasCalled;

            [GlobalCleanup]
            public async ValueTask GlobalCleanup()
            {
                await Task.Delay(1);

                WasCalled = true;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncGenericValueTaskGlobalSetupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericValueTaskGlobalSetup))).ToArrayAsync();

            Assert.True(AsyncGenericValueTaskGlobalSetup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncGenericValueTaskGlobalSetup
        {
            public static bool WasCalled;

            [GlobalSetup]
            public async ValueTask<int> GlobalSetup()
            {
                await Task.Delay(1);

                WasCalled = true;

                return 42;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncGenericValueTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericValueTaskGlobalCleanup))).ToArrayAsync();

            Assert.True(AsyncGenericValueTaskGlobalCleanup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncGenericValueTaskGlobalCleanup
        {
            public static bool WasCalled;

            [GlobalCleanup]
            public async ValueTask<int> GlobalCleanup()
            {
                await Task.Delay(1);

                WasCalled = true;

                return 42;
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        private class ValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
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

        [Fact]
        public async Task AsyncValueTaskBackedByIValueTaskSourceIsAwaitedProperly()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncValueTaskSource))).ToArrayAsync();

            Assert.True(AsyncValueTaskSource.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncValueTaskSource
        {
            private readonly ValueTaskSource<bool> valueTaskSource = new();

            public static bool WasCalled;

            [GlobalSetup]
            public ValueTask GlobalSetup()
            {
                valueTaskSource.Reset();
                Task.Delay(1).ContinueWith(_ =>
                {
                    WasCalled = true;
                    valueTaskSource.SetResult(true);
                });
                return new ValueTask(valueTaskSource, valueTaskSource.Token);
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task AsyncGenericValueTaskBackedByIValueTaskSourceIsAwaitedProperly()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericValueTaskSource))).ToArrayAsync();

            Assert.True(AsyncGenericValueTaskSource.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class AsyncGenericValueTaskSource
        {
            private readonly ValueTaskSource<int> valueTaskSource = new();

            public static bool WasCalled;

            [GlobalSetup]
            public ValueTask<int> GlobalSetup()
            {
                valueTaskSource.Reset();
                Task.Delay(1).ContinueWith(_ =>
                {
                    WasCalled = true;
                    valueTaskSource.SetResult(1);
                });
                return new ValueTask<int>(valueTaskSource, valueTaskSource.Token);
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task IteratorBodyExceptionsInAsyncEnumerableBenchmarksAreDiscovered()
        {
            // Without draining, calling the iterator method just allocates the state machine and the
            // body never runs — so the throw inside MoveNextAsync would silently pass validation.
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(ThrowingAsyncEnumerableBenchmark)))
                .ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.Contains(validationErrors, error => error.Message.Contains("This iterator throws"));
        }

        [Fact]
        public async Task ARefStructCurrentIsNotValidatedAndNotRefused()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(RefStructCurrentBenchmark)))
                .ToArrayAsync();

            // Says why it was skipped, and does not refuse it: IsCritical is what decides that, and FailOnError
            // would otherwise turn the explanation into a refusal.
            var skipped = Assert.Single(validationErrors);
            Assert.Contains("yields by-ref-like elements", skipped.Message);
            Assert.False(skipped.IsCritical);
        }

        [Fact]
        public async Task ARefStructParameterIsNotValidatedAndNotRefused()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(RefStructParameterBenchmark)))
                .ToArrayAsync();

            var skipped = Assert.Single(validationErrors);
            Assert.Contains("by-ref-like parameter", skipped.Message);
            Assert.False(skipped.IsCritical);
        }

        // The generated code passes a by-ref-like argument natively and runs; reflection cannot box one into the
        // args array, so the validator can only skip - it must not refuse.
        public class RefStructParameterBenchmark
        {
            public static IEnumerable<object> Values() { yield return new byte[] { 1, 2, 3 }; }

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public int Run(ReadOnlySpan<byte> bytes) => bytes.Length;
        }

        [Fact]
        public async Task ARefStructReturnIsNotValidatedAndNotRefused()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(RefStructReturnBenchmark)))
                .ToArrayAsync();

            var skipped = Assert.Single(validationErrors);
            Assert.Contains("returns by-ref-like value", skipped.Message);
            Assert.False(skipped.IsCritical);
        }

        // Reflection cannot box a ref struct to hand it back, so the method cannot even be invoked - the guard has
        // to come before the call, not around the result.
        public class RefStructReturnBenchmark
        {
            [Benchmark]
            public ReadOnlySpan<byte> Run() => default;
        }

        // Both validators read Current through reflection, which cannot hand back a ref struct - so neither can
        // validate this benchmark, and neither may refuse to run it. Its generated code reads Current strongly
        // typed and works.
        public class RefStructCurrentBenchmark
        {
            [Benchmark]
            public SpanEnumerable Enumerating() => default;

            public readonly struct SpanEnumerable
            {
                public SpanEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default) => new();
            }

            public struct SpanEnumerator
            {
                private int index;

                public ReadOnlySpan<byte> Current => default;

                public ValueTask<bool> MoveNextAsync() => new(index++ < 2);
            }
        }

        public class ThrowingAsyncEnumerableBenchmark
        {
            [Benchmark]
            public async IAsyncEnumerable<int> Throwing()
            {
                yield return 1;
                await Task.Yield();
                throw new Exception("This iterator throws");
            }
        }
    
        // An argument is passed to the benchmark method, so there is no member of that name to assign it to.
        // Reporting one is a validation error against a benchmark that runs perfectly well.
        [Fact]
        public async Task ArgumentsAreNotMistakenForMembersToAssign()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(WithArguments))).ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        public class WithArguments
        {
            [Params(2)]
            public int Parameter { get; set; }

            [Benchmark]
            [Arguments(1)]
            public int Bench(int value) => value + Parameter;
        }
}
}