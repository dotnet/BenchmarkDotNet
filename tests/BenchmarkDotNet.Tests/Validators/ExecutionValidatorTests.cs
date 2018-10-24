using System;
using System.Linq;
using System.Threading.Tasks;
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
        public void FailingConstructorsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingConstructor))).ToList();

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
        public void FailingGlobalSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalSetup))).ToList();

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
        public void FailingGlobalCleanupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalCleanup))).ToList();

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
        public void MultipleGlobalSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleGlobalSetups))).ToList();

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
        public void MultipleGlobalCleanupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleGlobalCleanups))).ToList();

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
        public void VirtualGlobalSetupsAreSupported()
        {
            Assert.False(OverridesGlobalSetup.WasCalled);
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(OverridesGlobalSetup)));

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
        public void VirtualGlobalCleanupsAreSupported()
        {
            Assert.False(OverridesGlobalCleanup.WasCalled);
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(OverridesGlobalCleanup)));

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
        public void NonFailingGlobalSetupsAreOmitted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(GlobalSetupThatRequiresParamsToBeSetFirst)));

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
        public void NonFailingGlobalCleanupsAreOmitted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(GlobalCleanupThatRequiresParamsToBeSetFirst)));

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
        public void MissingParamsAttributeThatMakesGlobalSetupsFailAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError
                .Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalSetupWhichShouldHaveHadParamsForField)))
                .ToList();

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
        public void MissingParamsAttributeThatMakesGlobalCleanupsFailAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError
                .Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalCleanupWhichShouldHaveHadParamsForField)))
                .ToList();

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
        public void NonPublicFieldsWithParamsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError
                .Validate(BenchmarkConverter.TypeToBenchmarks(typeof(NonPublicFieldWithParams)))
                .ToList();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("Fields marked with [Params] must be public", validationErrors.Single().Message);
        }

        public class NonPublicFieldWithParams
        {
#pragma warning disable CS0649
            [Params(1)]
            [UsedImplicitly]
            internal int Field;
#pragma warning restore CS0649

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void NonPublicPropertiesWithParamsAreDiscovered()
        {
            Assert.Throws<InvalidOperationException>(
                () => ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(NonPublicPropertyWithParams))));
        }

        public class NonPublicPropertyWithParams
        {
            [Params(1)]
            [UsedImplicitly]
            internal int Property { get; set; }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void PropertyWithoutPublicSetterParamsAreDiscovered()
        {
            Assert.Throws<InvalidOperationException>(
                () => ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(PropertyWithoutPublicSetterParams))));
        }

        public class PropertyWithoutPublicSetterParams
        {
            [Params(1)]
            [UsedImplicitly]
            internal int Property { get; }

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
            [Params]
            [UsedImplicitly]
            public int FieldWithoutValuesSpecified;

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void NonFailingBenchmarksAreOmitted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(NonFailingBenchmark)));

            Assert.Empty(validationErrors);
        }

        public class NonFailingBenchmark
        {
            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void FailingBenchmarksAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingBenchmark))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.Contains(validationErrors, error => error.Message.Contains("This benchmark throws"));
        }

        public class FailingBenchmark
        {
            [Benchmark]
            public void Throwing() => throw new Exception("This benchmark throws");
        }

        [Fact]
        public void MultipleParamsDoNotMultiplyGlobalSetup()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleParamsAndSingleGlobalSetup)));

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
        public void AsyncTaskGlobalSetupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncTaskGlobalSetup))).ToList();

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
        public void AsyncTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncTaskGlobalCleanup))).ToList();

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
        public void AsyncGenericTaskGlobalSetupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericTaskGlobalSetup))).ToList();

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
        public void AsyncGenericTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericTaskGlobalCleanup))).ToList();

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
        public void AsyncValueTaskGlobalSetupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncValueTaskGlobalSetup))).ToList();

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
        public void AsyncValueTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncValueTaskGlobalCleanup))).ToList();

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
        public void AsyncGenericValueTaskGlobalSetupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericValueTaskGlobalSetup))).ToList();

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
        public void AsyncGenericValueTaskGlobalCleanupIsExecuted()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncGenericValueTaskGlobalCleanup))).ToList();

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
    }
}