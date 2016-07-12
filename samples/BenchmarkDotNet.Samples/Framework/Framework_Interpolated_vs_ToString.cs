using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Framework
{
    // Inspired by https://twitter.com/Nick_Craver/status/702693060472414208
    public class Framework_Interpolated_vs_ToString
    {
        private long counter = 0;

        [Benchmark]
        public new string ToString()
        {
            return counter.ToString() + " ms";
        }

        [Benchmark]
        public string InterpolatedString()
        {
            return $"{counter} ms";
        }
    }
}
