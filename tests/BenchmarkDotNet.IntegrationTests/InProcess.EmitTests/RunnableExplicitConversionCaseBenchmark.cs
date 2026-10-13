using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    /// <summary>
    /// A by-ref-like parameter reached through an <c>explicit</c> operator rather than an implicit one. Written by
    /// hand rather than generated beside the other runnable cases, because every by-ref-like type the BCL offers
    /// declares its conversion implicitly, so the case only exists where a test declares it.
    /// </summary>
    /// <remarks>
    /// Both toolchains have to find the operator - the generated C# through the cast it writes when loading the
    /// argument, the emitter through the method it calls - so this covers both, and sits in the IL diff theory so
    /// the emitted IL is compared against Roslyn's for the shape rather than only run.
    /// </remarks>
    public class RunnableExplicitConversionCaseBenchmark
    {
        [Benchmark]
        [ArgumentsSource(nameof(Numbers))]
        public int ExplicitConversionCase(ReadOnlySpan<int> numbers)
        {
            if (numbers.Length != 3 || numbers[0] != 1)
                throw new ArgumentException("The argument values are incorrect!");

            return numbers.Length;
        }

        public IEnumerable<ExplicitlyConvertedToSpan> Numbers()
        {
            yield return new ExplicitlyConvertedToSpan { Values = [1, 2, 3] };
        }

        public class ExplicitlyConvertedToSpan
        {
            public required int[] Values;

            public static explicit operator ReadOnlySpan<int>(ExplicitlyConvertedToSpan instance) => instance.Values;
        }
    }
}
