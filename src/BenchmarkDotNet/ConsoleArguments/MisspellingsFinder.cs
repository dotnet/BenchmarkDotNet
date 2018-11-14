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

        //note this is an heuristic value, we suppose that user can make three misspellings
        private static int PossibleMisspellingCount => 3;

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
        }

        public string[] Find([NotNull] string userInput)
        {
            if (userInput == null) 
                throw new ArgumentNullException(nameof(userInput));
            return benchmarkNames.Where(name => GetLevenshteinDistance(userInput, name) <= PossibleMisspellingCount).ToArray();
        }

        private static int GetLevenshteinDistance(string string1, string string2)
        {
            var m = new int[string1.Length + 1, string2.Length + 1];

            for (int i = 0; i <= string1.Length; i++)
            {
                m[i, 0] = i;
            }

            for (int j = 0; j <= string2.Length; j++)
            {
                m[0, j] = j;
            }

            for (int i = 1; i <= string1.Length; i++)
            {
                for (int j = 1; j <= string2.Length; j++)
                {
                    int diff = (string1[i - 1] == string2[j - 1]) ? 0 : 1;

                    m[i, j] = Math.Min(Math.Min(m[i - 1, j] + 1,
                            m[i, j - 1] + 1),
                        m[i - 1, j - 1] + diff);
                }
            }

            return m[string1.Length, string2.Length];
        }
    }
}