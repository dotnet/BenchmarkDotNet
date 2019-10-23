using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class DeferredExecutionValidatorTests
    {
        public class ReturningIEnumerable
        {
            private IEnumerable field = Enumerable.Empty<int>();

            [Benchmark] public IEnumerable Benchmark() => field;
            [Benchmark] public Task<IEnumerable> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<IEnumerable> BenchmarkValueTask() => new ValueTask<IEnumerable>(field);
            [Benchmark] public ref IEnumerable BenchmarkRef() => ref field;
            [Benchmark] public ref readonly IEnumerable BenchmarkReadOnlyRef() => ref field;
        }

        public class ReturningIEnumerableOfInt
        {
            private IEnumerable<int> field = Enumerable.Empty<int>();

            [Benchmark] public IEnumerable<int> Benchmark() => field;
            [Benchmark] public Task<IEnumerable<int>> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<IEnumerable<int>> BenchmarkValueTask() => new ValueTask<IEnumerable<int>>(field);
            [Benchmark] public ref IEnumerable<int> BenchmarkRef() => ref field;
            [Benchmark] public ref readonly IEnumerable<int> BenchmarkReadOnlyRef() => ref field;
        }

        public class ReturningIQueryable
        {
            private IQueryable field = Enumerable.Empty<int>().AsQueryable();

            [Benchmark] public IQueryable Benchmark() => field;
            [Benchmark] public Task<IQueryable> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<IQueryable> BenchmarkValueTask() => new ValueTask<IQueryable>(field);
            [Benchmark] public ref IQueryable BenchmarkRef() => ref field;
            [Benchmark] public ref readonly IQueryable BenchmarkReadOnlyRef() => ref field;
        }

        public class ReturningIQueryableOfInt
        {
            private IQueryable<int> field = Enumerable.Empty<int>().AsQueryable();

            [Benchmark] public IQueryable<int> Benchmark() => Enumerable.Empty<int>().AsQueryable();
            [Benchmark] public Task<IQueryable<int>> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<IQueryable<int>> BenchmarkValueTask() => new ValueTask<IQueryable<int>>(field);
            [Benchmark] public ref IQueryable<int> BenchmarkRef() => ref field;
            [Benchmark] public ref readonly IQueryable<int> BenchmarkReadOnlyRef() => ref field;
        }

        public class ReturningLazyOfInt
        {
            private Lazy<int> field = new Lazy<int>(() => 0);

            [Benchmark] public Lazy<int> Benchmark() => field;
            [Benchmark] public Task<Lazy<int>> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<Lazy<int>> BenchmarkValueTask() => new ValueTask<Lazy<int>>(field);
            [Benchmark] public ref Lazy<int> BenchmarkRef() => ref field;
            [Benchmark] public ref readonly Lazy<int> BenchmarkReadOnlyRef() => ref field;
        }

        [Theory]
        [InlineData(typeof(ReturningIEnumerable))]
        [InlineData(typeof(ReturningIEnumerableOfInt))]
        [InlineData(typeof(ReturningIQueryable))]
        [InlineData(typeof(ReturningIQueryableOfInt))]
        [InlineData(typeof(ReturningLazyOfInt))]
        public void DeferredExecutionMeansError(Type returningDeferredExecutionResult)
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(returningDeferredExecutionResult);

            var validationErrors = DeferredExecutionValidator.FailOnError.Validate(benchmarks).ToArray();

            Assert.Equal(5, validationErrors.Count(error => error.IsCritical));
        }

        public class ReturningArray
        {
            private int[] field = Array.Empty<int>();

            [Benchmark] public int[] Benchmark() => field;
            [Benchmark] public Task<int[]> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<int[]> BenchmarkValueTask() => new ValueTask<int[]>(field);
            [Benchmark] public ref int[] BenchmarkRef() => ref field;
            [Benchmark] public ref readonly int[] BenchmarkReadOnlyRef() => ref field;
        }

        public class ReturningDictionary
        {
            private Dictionary<int, int> field = new Dictionary<int, int>();

            [Benchmark] public Dictionary<int, int> Benchmark() => field;
            [Benchmark] public Task<Dictionary<int, int>> BenchmarkTask() => Task.FromResult(field);
            [Benchmark] public ValueTask<Dictionary<int, int>> BenchmarkValueTask() => new ValueTask<Dictionary<int, int>>(field);
            [Benchmark] public ref Dictionary<int, int> BenchmarkRef() => ref field;
            [Benchmark] public ref readonly Dictionary<int, int> BenchmarkReadOnlyRef() => ref field;
        }

        [Theory]
        [InlineData(typeof(ReturningArray))]
        [InlineData(typeof(ReturningDictionary))]
        public void MaterializedCollectionsAreOk(Type returningMaterializedResult)
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(returningMaterializedResult);

            var validationErrors = DeferredExecutionValidator.FailOnError.Validate(benchmarks).ToArray();

            Assert.Empty(validationErrors);
        }
    }
}