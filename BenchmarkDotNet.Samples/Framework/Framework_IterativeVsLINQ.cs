using BenchmarkDotNet.Attributes;
using System;
using System.Linq;

namespace BenchmarkDotNet.Samples.Framework
{
    public class Framework_IterativeVsLINQ
    {
        private string[] parameters = new[]
        {
            "One",
            "Two",
            "Three",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "Ten"
        };

        [Benchmark]
        public string Iterative()
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (string.Equals(parameter, "Five", StringComparison.OrdinalIgnoreCase))
                {
                    return parameter;
                }
            }
            return null;
        }

        [Benchmark]
        public string LINQ()
        {
            return parameters.FirstOrDefault(p => string.Equals(p, "Five", StringComparison.OrdinalIgnoreCase));
        }
    }
}
