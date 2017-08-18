using System;
using System.Linq;
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
        public void FailingGlobalSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingGlobalSetup))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Failed to execute [GlobalSetup]"));
        }

        public class FailingGlobalSetup
        {
            [GlobalSetup]
            public void Failing()
            {
                throw new Exception("This one fails");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void MultipleGlobalSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleGlobalSetups))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Only single [GlobalSetup] method is allowed per type"));
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
            public virtual void GlobalSetup()
            {
                throw new Exception("should not be executed when overridden");
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        public class OverridesGlobalSetup : BaseClassWithThrowingGlobalSetup
        {
            public static bool WasCalled;

            [GlobalSetup]
            public override void GlobalSetup()
            {
                WasCalled = true;
            }
        }

        [Fact]
        public void NonFailingGlobalSetupsAreOmmited()
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
                if (Field == default(int))
                    throw new Exception("this should have never happened");
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
            Assert.True(validationErrors.Single().Message.StartsWith("Failed to execute [GlobalSetup]"));
        }

        public class FailingGlobalSetupWhichShouldHaveHadParamsForField
        {
            [UsedImplicitly]
            public int Field;

            [GlobalSetup]
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
    }
}