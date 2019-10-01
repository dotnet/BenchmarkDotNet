using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    public class IntroDeferredExecution
    {
        private readonly int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        private readonly Consumer consumer = new Consumer();

        /// <summary>
        /// this benchmark returns a deferred LINQ query which is NOT executed
        /// so the benchmark measures the cost of creating the query, not the actual execution
        /// this is WRONG
        /// You can read more about LINQ and Deferred Execution <see href="https://blogs.msdn.microsoft.com/charlie/2007/12/10/linq-and-deferred-execution/">here</see>
        /// </summary>
        /// <returns>deferred LINQ query</returns>
        [Benchmark]
        public IEnumerable<int> Wrong() => from number in numbers orderby number descending select number;

        /// <summary>
        /// this benchmark uses .Consume extension method which executes given deferred query and consumes its result
        /// so the benchmark measures the cost of creating the query and executing it
        /// </summary>
        [Benchmark]
        public void Ok() => (from number in numbers orderby number descending select number).Consume(consumer);
    }
}