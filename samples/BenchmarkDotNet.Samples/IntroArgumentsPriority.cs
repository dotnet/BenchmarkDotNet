using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroArgumentsPriority
    {
        [Params(100, Priority = 0)] // Argument priority can be combined with Params priority
        public int A { get; set; }

        [Arguments(5, Priority = -10)] // Define priority just once for multiple argument attributes
        [Arguments(10)]
        [Arguments(20)]
        [Benchmark]
        public void Benchmark(int b) => Thread.Sleep(A + b);

        [Benchmark]
        [ArgumentsSource(nameof(Numbers), Priority = 10)]
        public void ManyArguments(int c, int d) => Thread.Sleep(A + c + d);

        public IEnumerable<object[]> Numbers()
        {
            yield return new object[] { 1, 2 };
        }
    }
}