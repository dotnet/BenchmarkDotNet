using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Text.RegularExpressions;

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
            public List<int> Foo() => [1, 2, 3];

            [Benchmark]
            public int[] Bar() => [1, 2, 3];
        }

        [Fact]
        public async Task InconsistentCollectionsAreDetected()
            => await AssertInconsistent<InconsistentCollectionReturnType>();

        public class InconsistentCollectionReturnType
        {
            [Benchmark]
            public List<int> Foo() => [1, 2, 3];

            [Benchmark]
            public int[] Bar() => [1, 42, 3];
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
            public bool Equals(CustomEquatableB? other) => other != null;

            public override bool Equals(object? obj) => false; // Intentionally bad implementation

            public override int GetHashCode() => 0;
        }

        public class CustomEquatableB : IEquatable<CustomEquatableA>
        {
            public bool Equals(CustomEquatableA? other) => other != null;

            public override bool Equals(object? obj) => false; // Intentionally bad implementation

            public override int GetHashCode() => 0;
        }

        [Fact]
        public async Task ConsistentAsyncEnumerablesAreOmitted()
            => await AssertConsistent<ConsistentAsyncEnumerableReturnType>();

        public class ConsistentAsyncEnumerableReturnType
        {
            [Benchmark]
            public async IAsyncEnumerable<int> Foo()
            {
                await Task.Yield();
                yield return 1;
                yield return 2;
                yield return 3;
            }

            [Benchmark]
            public async IAsyncEnumerable<int> Bar()
            {
                yield return 1;
                yield return 2;
                await Task.Yield();
                yield return 3;
            }
        }

        [Fact]
        public async Task InconsistentAsyncEnumerablesAreDetected()
            => await AssertInconsistent<InconsistentAsyncEnumerableReturnType>();

        public class InconsistentAsyncEnumerableReturnType
        {
            [Benchmark]
            public async IAsyncEnumerable<int> Foo()
            {
                await Task.Yield();
                yield return 1;
                yield return 2;
                yield return 3;
            }

            [Benchmark]
            public async IAsyncEnumerable<int> Bar()
            {
                await Task.Yield();
                yield return 1;
                yield return 42;
                yield return 3;
            }
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

        // #2083
        [Fact]
        public async Task GlobalSetupRunsForEachParamsValue()
            => await AssertConsistent<GlobalSetupPerParamsValue>();

        public class GlobalSetupPerParamsValue
        {
            [Params(2, 3)]
            public int N { get; set; }

            private int _expected;

            [GlobalSetup]
            public void Setup() => _expected = N * 10;

            [Benchmark]
            public int FromSetup() => _expected;

            [Benchmark]
            public int FromParam() => N * 10;
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
    
        [Fact]
        public async Task ARefStructCurrentIsLeftOutOfTheComparisonRatherThanRefused()
        {
            // Comparing return values means holding every element, which a ref struct cannot be - so this benchmark
            // has no comparable value. That leaves it out of the comparison; it must not fail it.
            var validationErrors = await ReturnValueValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(RefStructCurrentBenchmark)))
                .ToArrayAsync();

            var skipped = Assert.Single(validationErrors);
            Assert.Contains("yields by-ref-like elements", skipped.Message);
            Assert.False(skipped.IsCritical);
        }

        [Fact]
        public async Task ARefStructParameterIsLeftOutOfTheComparisonRatherThanRefused()
        {
            var validationErrors = await ReturnValueValidator.FailOnError
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
        public async Task ARefStructReturnIsLeftOutOfTheComparisonRatherThanRefused()
        {
            var validationErrors = await ReturnValueValidator.FailOnError
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
}
}