using System;

namespace BenchmarkDotNet.Mathematics
{
    internal static class RandomExtensions
    {
        // See https://stackoverflow.com/questions/218060/random-gaussian-variables
        public static double NextGaussian(this Random random, double mean = 0, double stdDev = 1)
        {
            double stdDevFactor = Math.Sqrt(-2.0 * Math.Log(random.NextDouble())) * Math.Sin(2.0 * Math.PI * random.NextDouble());
            return mean + stdDev * stdDevFactor;
        }
    }
}