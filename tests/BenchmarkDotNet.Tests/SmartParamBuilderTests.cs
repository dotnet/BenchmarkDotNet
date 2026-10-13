using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests
{
    public class SmartParamBuilderTests
    {
        // A ValueTuple holds seven items and nests the rest in a Rest that is itself a ValueTuple, so reaching the
        // nth item walks one Rest per seven. The generated code and discovery both read the path from here, which
        // is what keeps them naming the same item.
        [Theory]
        [InlineData(0, "Item1")]
        [InlineData(6, "Item7")]
        // The eighth item is the first behind a Rest, and the fourteenth is the last one reached through a single hop.
        [InlineData(7, "Rest.Item1")]
        [InlineData(13, "Rest.Item7")]
        // The fifteenth opens a second Rest, which a walk that stops at the first would read as the eighth.
        [InlineData(14, "Rest.Rest.Item1")]
        [InlineData(21, "Rest.Rest.Rest.Item1")]
        public void TupleItemPathWalksOneRestPerSevenItems(int index, string expected)
            => Assert.Equal(expected, string.Join(".", SmartParamBuilder.TupleItemPath(index)));

        // Nesting is not limited to one level: both the walk that collects the item types and the one that reads a
        // row take a Rest at a time, so a tuple is as long as the benchmark has parameters. Fifteen and twenty-two
        // are the arities that open the second and third Rest.
        [Theory]
        [InlineData(typeof(FifteenArguments))]
        [InlineData(typeof(TwentyTwoArguments))]
        public void ATupleIsReadThroughAsManyRestsAsItHas(Type benchmarkType)
        {
            var parameters = Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases).Parameters.Items;

            // Every argument is the ordinal of its own position, so a misread item shows up as the wrong number
            // rather than as a type error.
            Assert.Equal(
                Enumerable.Range(1, parameters.Count).ToArray(),
                parameters.Select(parameter => parameter.Value).Cast<int>().ToArray());
        }

        public class FifteenArguments
        {
            public static IEnumerable<(int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)> Rows()
            {
                yield return (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            }

            [Benchmark]
            [ArgumentsSource(nameof(Rows))]
            public int Run(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o)
                => a + b + c + d + e + f + g + h + i + j + k + l + m + n + o;
        }

        public class TwentyTwoArguments
        {
            public static IEnumerable<(int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)> Rows()
            {
                yield return (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22);
            }

            [Benchmark]
            [ArgumentsSource(nameof(Rows))]
            public int Run(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k,
                int l, int m, int n, int o, int p, int q, int r, int s, int t, int u, int v)
                => a + b + c + d + e + f + g + h + i + j + k + l + m + n + o + p + q + r + s + t + u + v;
        }
    }
}
