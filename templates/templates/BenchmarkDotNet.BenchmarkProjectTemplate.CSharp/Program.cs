using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace _BenchmarkProjectName_
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IEnumerable<Summary> summaries =
                BenchmarkSwitcher.FromTypes(new []{ typeof($(BenchmarkName)) }).Run(args);
        }
    }
}
