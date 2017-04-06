using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ExecutionValidatorTests
    {
        [Fact]
        public void FailingConsturctorsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingConsturctor))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Unable to create instance of FailingConsturctor"));
        }

        public class FailingConsturctor
        {
            public FailingConsturctor()
            {
                throw new Exception("This one fails");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void FailingSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingSetup))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Failed to execute [Setup]"));
        }

        public class FailingSetup
        {
            [Setup]
            public void Failing()
            {
                throw new Exception("This one fails");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void MultipleSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleSetups))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Only single [Setup] method is allowed per type"));
        }

        public class MultipleSetups
        {
            [Setup]
            public void First() { }

            [Setup]
            public void Second() { }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void VirtualSetupsAreSupported()
        {
            Assert.False(OverridesSetup.WasCalled);
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(OverridesSetup)));

            Assert.True(OverridesSetup.WasCalled);
            Assert.Empty(validationErrors);
        }

        public class BaseClassWithThrowingSetup
        {
            [Setup]
            public virtual void Setup()
            {
                throw new Exception("should not be executed when overridden");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        public class OverridesSetup : BaseClassWithThrowingSetup
        {
            public static bool WasCalled;

            [Setup]
            public override void Setup()
            {
                WasCalled = true;
            }
        }

        [Fact]
        public void NonFailingSetupsAreOmmited()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(SetupThatRequiresParamsToBeSetFirst)));

            Assert.Empty(validationErrors);
        }

        public class SetupThatRequiresParamsToBeSetFirst
        {
            [Params(100)]
            [UsedImplicitly]
            public int Field;

            [Setup]
            public void Failing()
            {
                if (Field == default(int))
                    throw new Exception("this should have never happened");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void MissingParamsAttributeThatMakesSetupsFailAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError
                .Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingSetupWhichShouldHaveHadParamsForField)))
                .ToList();

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Failed to execute [Setup]"));
        }

        public class FailingSetupWhichShouldHaveHadParamsForField
        {
            [UsedImplicitly]
            public int Field;

            [Setup]
            public void Failing()
            {
                if (Field == default(int))
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
            Assert.True(validationErrors.Single().Message.StartsWith("Fields marked with [Params] must be public"));
        }

        public class NonPublicFieldWithParams
        {
            [Params(1)]
            [UsedImplicitly]
            internal int Field;

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
            Assert.Empty(BenchmarkConverter.TypeToBenchmarks(typeof(FieldsWithoutParamsValues)));
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
        public void NonFailingBenchmarksAreOmmited()
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
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingBenchmark)));

            Assert.NotEmpty(validationErrors);
        }

        public class FailingBenchmark
        {
            [Benchmark]
            public void Throwing()
            {
                throw new Exception("This benchmark throws");
            }
        }

        [Fact]
        public void MultipleParamsDoNotMultiplySetup()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleParamsAndSingleSetup)));

            Assert.Empty(validationErrors);
        }

        public class MultipleParamsAndSingleSetup
        {
            [Params(1, 2)]
            [UsedImplicitly]
            public int Field;

            [Setup]
            public void Single() { }

            [Benchmark]
            public void NonThrowing() { }
        }
    }
}