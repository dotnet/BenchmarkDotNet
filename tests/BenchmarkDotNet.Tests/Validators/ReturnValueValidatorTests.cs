using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public async Task ThrowingBenchmarksAreDiscovered()
        {
            var validationErrors = await ReturnValueValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(ThrowingBenchmark))).ToArrayAsync();

            Assert.Single(validationErrors);
            Assert.Contains("Oops, sorry", validationErrors.Single().Message);
        }

        public class ThrowingBenchmark
        {
            [Benchmark]
            public void Foo() => throw new InvalidOperationException("Oops, sorry");
        }

        [Fact]
        public async Task InconsistentReturnValuesAreDiscovered()
        {
            var validationErrors = await AssertInconsistent<InconsistentResults>();
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
        public async Task NoDuplicateResultsArePrinted()
        {
            var validationErrors = await AssertInconsistent<InconsistentResultsWithMultipleJobs>();
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
        public async Task ConsistentReturnValuesAreOmitted()
            => await AssertConsistent<ConsistentResults>();

        public class ConsistentResults
        {
            [Benchmark]
            public int Foo() => 42;

            [Benchmark]
            public int Bar() => 42;
        }

        [Fact]
        public async Task BenchmarksWithOnlyVoidMethodsAreOmitted()
            => await AssertConsistent<VoidMethods>();

        public class VoidMethods
        {
            [Benchmark]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }

        [Fact]
        public async Task VoidMethodsAreIgnored()
            => await AssertConsistent<ConsistentResultsWithVoidMethod>();

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
        public async Task ConsistentReturnValuesInParameterGroupAreOmitted()
            => await AssertConsistent<ConsistentResultsPerParameterGroup>();

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
        public async Task InconsistentReturnValuesInParameterGroupAreDetected()
        {
            var validationErrors = await AssertInconsistent<InconsistentResultsPerParameterGroup>();
            Assert.Equal(2, validationErrors.Length);
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
        public async Task ConsistentCollectionsAreOmitted()
            => await AssertConsistent<ConsistentCollectionReturnType>();

        public class ConsistentCollectionReturnType
        {
            [Benchmark]
            public List<int> Foo() => new List<int> { 1, 2, 3 };

            [Benchmark]
            public int[] Bar() => new[] { 1, 2, 3 };
        }

        [Fact]
        public async Task InconsistentCollectionsAreDetected()
            => await AssertInconsistent<InconsistentCollectionReturnType>();

        public class InconsistentCollectionReturnType
        {
            [Benchmark]
            public List<int> Foo() => new List<int> { 1, 2, 3 };

            [Benchmark]
            public int[] Bar() => new[] { 1, 42, 3 };
        }

        [Fact]
        public async Task ConsistentDictionariesAreOmitted()
            => await AssertConsistent<ConsistentDictionaryReturnType>();

        public class ConsistentDictionaryReturnType
        {
            [Benchmark]
            public Dictionary<string, int> Foo() => new Dictionary<string, int> { { "Foo", 1 }, { "Bar", 2 }, { "Baz", 3 } };

            [Benchmark]
            public Dictionary<string, int> Bar() => new Dictionary<string, int> { ["Baz"] = 3, ["Foo"] = 1, ["Bar"] = 2 };
        }

        [Fact]
        public async Task InconsistentDictionariesAreDetected()
            => await AssertInconsistent<InconsistentDictionaryReturnType>();

        public class InconsistentDictionaryReturnType
        {
            [Benchmark]
            public Dictionary<string, int> Foo() => new Dictionary<string, int> { { "Foo", 1 }, { "Bar", 42 }, { "Baz", 3 } };

            [Benchmark]
            public Dictionary<string, int> Bar() => new Dictionary<string, int> { ["Baz"] = 3, ["Foo"] = 1, ["Bar"] = 2 };
        }

        [Fact]
        public async Task ConsistentCustomEquatableImplementationIsOmitted()
            => await AssertConsistent<ConsistentCustomEquatableReturnType>();

        public class ConsistentCustomEquatableReturnType
        {
            [Benchmark]
            public CustomEquatableA Foo() => new CustomEquatableA();

            [Benchmark]
            public CustomEquatableB Bar() => new CustomEquatableB();
        }

        [Fact]
        public async Task InconsistentCustomEquatableImplementationIsDetected()
            => await AssertInconsistent<InconsistentCustomEquatableReturnType>();

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
        public async Task ConsistentBenchmarksAlteringParameterAreOmitted()
            => await AssertConsistent<ConsistentAlterParam>();

        public class ConsistentAlterParam
        {
            [Params(10, 20, 30)]
            public int Value { get; set; }

            [Benchmark]
            public int Foo() => ++Value;

            [Benchmark]
            public int Bar() => ++Value;
        }

        private static async Task AssertConsistent<TBenchmark>()
        {
            var validationErrors = await ReturnValueValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(TBenchmark))).ToArrayAsync();

            Assert.Empty(validationErrors);
        }

        private static async Task<ValidationError[]> AssertInconsistent<TBenchmark>()
        {
            var validationErrors = await ReturnValueValidator.FailOnError.ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(TBenchmark))).ToArrayAsync();

            Assert.NotEmpty(validationErrors);
            Assert.All(validationErrors, error => Assert.StartsWith(ErrorMessagePrefix, error.Message));

            return validationErrors;
        }
    }
}