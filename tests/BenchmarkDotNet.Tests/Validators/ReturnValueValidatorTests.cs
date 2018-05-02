using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class ReturnValueValidatorTests
    {
        private const string ErrorMessagePrefix = "Inconsistent benchmark return values";

        [Fact]
        public void InconsistentReturnValuesAreDiscovered()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(InconsistentResults))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith(ErrorMessagePrefix, validationErrors.Single().Message);
        }

        public class InconsistentResults
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 41;
        }

        [Fact]
        public void ConsistentReturnValuesAreOmitted()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(ConsistentResults))).ToList();

            Assert.Empty(validationErrors);
        }

        public class ConsistentResults
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 42;
        }

        [Fact]
        public void BenchmarksWithOnlyVoidMethodsAreOmitted()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(VoidMethods))).ToList();

            Assert.Empty(validationErrors);
        }

        public class VoidMethods
        {
            [Benchmark]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }

        [Fact]
        public void VoidMethodsAreIgnored()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(ConsistentResultsWithVoidMethod))).ToList();

            Assert.Empty(validationErrors);
        }

        public class ConsistentResultsWithVoidMethod
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 42;

            [Benchmark]
            public void Baz() { }
        }

        [Fact]
        public void ConsistentReturnValuesInParameterGroupAreOmitted()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(ConsistentResultsPerParameterGroup))).ToList();

            Assert.Empty(validationErrors);
        }

        public class ConsistentResultsPerParameterGroup
        {
            [Params(1, 2, 3)]
            public int Value { get; set; }

            [Benchmark]
            public int Foo() => Value;

            [Benchmark]
            public int Bar() => Value;
        }

        [Fact]
        public void InconsistentReturnValuesInParameterGroupAreDetected()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(InconsistentResultsPerParameterGroup))).ToList();

            Assert.Equal(2, validationErrors.Count);
            Assert.All(validationErrors, error => Assert.StartsWith(ErrorMessagePrefix, error.Message));
        }

        public class InconsistentResultsPerParameterGroup
        {
            [Params(1, 2, 3)]
            public int Value { get; set; }

            [Benchmark]
            public int Foo() => Value;

            [Benchmark]
            public int Bar() => 2;
        }
    }
}