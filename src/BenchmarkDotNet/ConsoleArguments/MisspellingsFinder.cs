using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.ConsoleArguments
{
    public class MisspellingsFinder
    {
        private readonly List<string> benchmarkNames = new List<string>();

        public MisspellingsFinder(IReadOnlyList<Type> benchmarks)
        {
            foreach (var benchmark in benchmarks)
            {
                benchmarkNames.AddRange(benchmark.GetMethods()
                    .Where(methodInfo => methodInfo.HasAttribute<BenchmarkAttribute>())
                    .Select(methodInfo => $"{benchmark.FullName}.{methodInfo.Name}"));
            }

            benchmarkNames.AddRange(benchmarks.Select(bn => bn.FullName).Distinct());
            benchmarkNames.AddRange(benchmarks.Select(bn => bn.Namespace).Distinct());
            benchmarkNames.AddRange(benchmarks.Select(bn => bn.Name).Distinct());
        }

        public string[] Find([NotNull] string userInput)
        {
            if (userInput == null) 
                throw new ArgumentNullException(nameof(userInput));
            var calculator = new LevenshteinDistanceCalculator();
            return benchmarkNames.OrderBy(bn => calculator.Calculate(userInput, bn)).ToArray();
        }
    }
}