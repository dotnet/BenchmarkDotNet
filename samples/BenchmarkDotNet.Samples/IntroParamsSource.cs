using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroParamsSource
    {
        // property with public setter
        [ParamsSource(nameof(ValuesForA))]
        public int A { get; set; }

        // public field
        [ParamsSource(nameof(ValuesForB))]
        public int B;

        // public property
        public IEnumerable<int> ValuesForA => [100, 200];

        // public static method
        public static IEnumerable<int> ValuesForB() => [10, 20];

        // public field getting its params from a method in another type
        [ParamsSource(typeof(ParamsValues), nameof(ParamsValues.ValuesForC))]
        public int C;

        // public field getting its params from an asynchronous source, which BenchmarkDotNet awaits.
        // Useful when the values can only be produced asynchronously - loaded from a database or a remote
        // service - without resorting to blocking sync-over-async in the source.
        [ParamsSource(nameof(ValuesForD))]
        public int D;

        // the source method may take an optional [EnumeratorCancellation] CancellationToken. It receives the
        // benchmark's cancellation token while the values are enumerated, so the async work can be cancelled.
        public static async IAsyncEnumerable<int> ValuesForD([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            yield return 1;
            yield return 2;
        }

        [Benchmark]
        public void Benchmark() => Thread.Sleep(A + B + C + D + 5);
    }

    public static class ParamsValues
    {
        public static IEnumerable<int> ValuesForC() => [1000, 2000];
    }
}