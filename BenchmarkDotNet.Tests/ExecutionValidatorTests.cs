using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ExecutionValidatorTests
    {
        [Fact]
        public void FailingConsturctorsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingConsturctor)));

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Unable to create instance of FailingConsturctor"));
        }

        public class FailingConsturctor
        {
            public FailingConsturctor() { throw new Exception("This one fails"); }

            [Benchmark] public void NonThrowing() { }
        }

        [Fact]
        public void FailingSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingSetup)));

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Failed to execute [Setup]"));
        }

        public class FailingSetup
        {
            [Setup] public void Failing() { throw new Exception("This one fails"); }

            [Benchmark] public void NonThrowing() { }
        }

        [Fact]
        public void MultipleSetupsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(MultipleSetups)));

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
        public void NonFailingSetupsAreOmmited()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(SetupThatRequiresParamsToBeSetFirst)));

            Assert.Empty(validationErrors);
        }

        public class SetupThatRequiresParamsToBeSetFirst
        {
            [Params(100)] public int Field;

            [Setup]
            public void Failing()
            {
                if (Field == default(int))
                {
                    throw new Exception("this should have never happened");
                }
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void MissingParamsAttributeThatMakesSetupsFailAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(FailingSetupWhichShouldHaveHadParamsForField)));

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Failed to execute [Setup]"));
        }

        public class FailingSetupWhichShouldHaveHadParamsForField
        {
            public int Field;

            [Setup]
            public void Failing()
            {
                if (Field == default(int))
                {
                    throw new Exception("Field is missing Params attribute");
                }
            }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void NonPublicFieldsWithParamsAreDiscovered()
        {
            var validationErrors = ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(NonPublicFieldWithParams)));

            Assert.NotEmpty(validationErrors);
            Assert.True(validationErrors.Single().Message.StartsWith("Fields marked with [Params] must be public"));
        }

        public class NonPublicFieldWithParams
        {
            [Params(1)]
            internal int Field;

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void NonPublicPropertiesWithParamsAreDiscovered()
        {
            Assert.Throws<InvalidOperationException>(() => ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(NonPublicPropertyWithParams))));
        }

        public class NonPublicPropertyWithParams
        {
            [Params(1)]
            internal int Property { get; set; }

            [Benchmark]
            public void NonThrowing() { }
        }

        [Fact]
        public void PropertyWithoutPublicSetterParamsAreDiscovered()
        {
            Assert.Throws<InvalidOperationException>(() => ExecutionValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(PropertyWithoutPublicSetterParams))));
        }

        public class PropertyWithoutPublicSetterParams
        {
            [Params(1)]
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
            public void Throwing() { throw new Exception("This benchmark throws");}
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
            public int Field;

            [Setup]
            public void Single() { }

            [Benchmark]
            public void NonThrowing() { }
        }

    }
}