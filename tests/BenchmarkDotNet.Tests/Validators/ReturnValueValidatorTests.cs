using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public void ThrowingBenchmarksAreDiscovered()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(ThrowingBenchmark))).ToList();

            Assert.Single(validationErrors);
            Assert.Contains("Oops, sorry", validationErrors.Single().Message);
        }

        public class ThrowingBenchmark
        {
            [Benchmark]
            public void Foo() => throw new InvalidOperationException("Oops, sorry");
        }

        [Fact]
        public void InconsistentReturnValuesAreDiscovered()
        {
            var validationErrors = AssertInconsistent<InconsistentResults>();
            Assert.Single(validationErrors);
        }

        public class InconsistentResults
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 41;
        }

        [Fact]
        public void NoDuplicateResultsArePrinted()
        {
            var validationErrors = AssertInconsistent<InconsistentResultsWithMultipleJobs>();
            Assert.Single(validationErrors);

            var allInstancesOfFoo = Regex.Matches(validationErrors.Single().Message, @"\bFoo\b");
            Assert.Single(allInstancesOfFoo);
        }

        [DryJob, InProcess]
        public class InconsistentResultsWithMultipleJobs
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 41;
        }

        [Fact]
        public void ConsistentReturnValuesAreOmitted()
            => AssertConsistent<ConsistentResults>();

        public class ConsistentResults
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 42;
        }

        [Fact]
        public void BenchmarksWithOnlyVoidMethodsAreOmitted()
            => AssertConsistent<VoidMethods>();

        public class VoidMethods
        {
            [Benchmark]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }

        [Fact]
        public void VoidMethodsAreIgnored()
            => AssertConsistent<ConsistentResultsWithVoidMethod>();

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
            => AssertConsistent<ConsistentResultsPerParameterGroup>();

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
            var validationErrors = AssertInconsistent<InconsistentResultsPerParameterGroup>();
            Assert.Equal(2, validationErrors.Count);
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

        [Fact]
        public void ConsistentCollectionsAreOmitted()
            => AssertConsistent<ConsistentCollectionReturnType>();

        public class ConsistentCollectionReturnType
        {
            [Benchmark]
            public List<int> Foo() => new List<int> { 1, 2, 3 };

            [Benchmark]
            public int[] Bar() => new[] { 1, 2, 3 };
        }

        [Fact]
        public void InconsistentCollectionsAreDetected()
            => AssertInconsistent<InconsistentCollectionReturnType>();

        public class InconsistentCollectionReturnType
        {
            [Benchmark]
            public List<int> Foo() => new List<int> { 1, 2, 3 };

            [Benchmark]
            public int[] Bar() => new[] { 1, 42, 3 };
        }

        [Fact]
        public void ConsistentDictionariesAreOmitted()
            => AssertConsistent<ConsistentDictionaryReturnType>();

        public class ConsistentDictionaryReturnType
        {
            [Benchmark]
            public Dictionary<string, int> Foo() => new Dictionary<string, int> { { "Foo", 1 }, { "Bar", 2 }, { "Baz", 3 } };

            [Benchmark]
            public Dictionary<string, int> Bar() => new Dictionary<string, int> { ["Baz"] = 3, ["Foo"] = 1, ["Bar"] = 2 };
        }

        [Fact]
        public void InconsistentDictionariesAreDetected()
            => AssertInconsistent<InconsistentDictionaryReturnType>();

        public class InconsistentDictionaryReturnType
        {
            [Benchmark]
            public Dictionary<string, int> Foo() => new Dictionary<string, int> { { "Foo", 1 }, { "Bar", 42 }, { "Baz", 3 } };

            [Benchmark]
            public Dictionary<string, int> Bar() => new Dictionary<string, int> { ["Baz"] = 3, ["Foo"] = 1, ["Bar"] = 2 };
        }

        [Fact]
        public void ConsistentCustomEquatableImplementationIsOmitted()
            => AssertConsistent<ConsistentCustomEquatableReturnType>();

        public class ConsistentCustomEquatableReturnType
        {
            [Benchmark]
            public CustomEquatableA Foo() => new CustomEquatableA();

            [Benchmark]
            public CustomEquatableB Bar() => new CustomEquatableB();
        }

        [Fact]
        public void InconsistentCustomEquatableImplementationIsDetected()
            => AssertInconsistent<InconsistentCustomEquatableReturnType>();

        public class InconsistentCustomEquatableReturnType
        {
            [Benchmark]
            public CustomEquatableA Foo() => new CustomEquatableA();

            [Benchmark]
            public CustomEquatableA Bar() => new CustomEquatableA();
        }

        public class CustomEquatableA : IEquatable<CustomEquatableB>
        {
            public bool Equals(CustomEquatableB other) => other != null;

            public override bool Equals(object obj) => false; // Intentionally bad implementation

            public override int GetHashCode() => 0;
        }

        public class CustomEquatableB : IEquatable<CustomEquatableA>
        {
            public bool Equals(CustomEquatableA other) => other != null;

            public override bool Equals(object obj) => false; // Intentionally bad implementation

            public override int GetHashCode() => 0;
        }

        [Fact]
        public void ConsistentBenchmarksAlteringParameterAreOmitted()
            => AssertConsistent<ConsistentAlterParam>();

        public class ConsistentAlterParam
        {
            [Params(10, 20, 30)]
            public int Value { get; set; }

            [Benchmark]
            public int Foo() => ++Value;

            [Benchmark]
            public int Bar() => ++Value;
        }

        private static void AssertConsistent<TBenchmark>()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(TBenchmark))).ToList();

            Assert.Empty(validationErrors);
        }

        private static List<ValidationError> AssertInconsistent<TBenchmark>()
        {
            var validationErrors = ReturnValueValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(typeof(TBenchmark))).ToList();

            Assert.NotEmpty(validationErrors);
            Assert.All(validationErrors, error => Assert.StartsWith(ErrorMessagePrefix, error.Message));

            return validationErrors;
        }
    }
}