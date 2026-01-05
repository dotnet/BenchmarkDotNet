using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Xunit;

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
        public async Task MultipleGlobalSetupsAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleGlobalSetups))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Only single [GlobalSetup] method is allowed per type", validationErrors.Single().Message);
        }

        public class MultipleGlobalSetups
        {
            [GlobalSetup]
            public void First() { }

            [GlobalSetup]
            public void Second() { }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public async Task MultipleGlobalCleanupsAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleGlobalCleanups))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Only single [GlobalCleanup] method is allowed per type", validationErrors.Single().Message);
        }

        public class MultipleGlobalCleanups
        {
            [GlobalCleanup]
            public void First() { }

            [GlobalCleanup]
            public void Second() { }

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
        public async Task NonPublicFieldsWithParamsAreDiscovered()
        {
            var validationErrors = await ExecutionValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(NonPublicFieldWithParams)))
                .ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Fields marked with [Params] must be public", validationErrors.Single().Message);
        }

        public class NonPublicFieldWithParams
        {
#pragma warning disable CS0649, BDN1202
            [Params(1)]
            [UsedImplicitly]
            internal int Field;
#pragma warning restore CS0649, BDN1202

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
            void IValueTaskSource<T>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
            void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
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
    }
}