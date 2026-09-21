using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroArgumentsSource
    {
        [Benchmark]
        [ArgumentsSource(nameof(Numbers))]
        public double ManyArguments(double x, double y) => Math.Pow(x, y);

        // For several arguments the source yields one element per case holding all of them: a ValueTuple naming each parameter's type.
        public IEnumerable<(double x, double y)> Numbers()
        {
            yield return (1.0, 1.0);
            yield return (2.0, 2.0);
            yield return (4.0, 4.0);
            yield return (10.0, 10.0);
        }

        [Benchmark]
        [ArgumentsSource(typeof(BenchmarkArguments), nameof(BenchmarkArguments.TimeSpans))] // when the arguments come from a different type, specify that type here
        public void SingleArgument(TimeSpan time) => Thread.Sleep(time);

        [Benchmark]
        [ArgumentsSource(nameof(NumbersAsync))]
        public double AsyncSourcedArguments(double x, double y) => Math.Pow(x, y);

        // the source may be an IAsyncEnumerable, which BenchmarkDotNet awaits, so the values can be produced
        // asynchronously without resorting to blocking sync-over-async in the source. It may take an optional
        // [EnumeratorCancellation] CancellationToken, which receives the benchmark's cancellation token while
        // the values are enumerated.
        public static async IAsyncEnumerable<(double x, double y)> NumbersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            yield return (1.0, 1.0);
            yield return (2.0, 2.0);
        }
    }

    public static class BenchmarkArguments
    {
        public static IEnumerable<TimeSpan> TimeSpans() // For single argument it's an IEnumerable of the parameter's type.
        {
            yield return TimeSpan.FromMilliseconds(10);
            yield return TimeSpan.FromMilliseconds(100);
        }
    }
}