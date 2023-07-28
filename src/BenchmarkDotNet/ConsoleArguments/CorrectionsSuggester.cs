using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.ConsoleArguments
{
    public class CorrectionsSuggester
    {
        // note This is a heuristic value, we suppose that user can make three or fewer typos.
        private static int PossibleTyposCount => 3;
        private readonly HashSet<string> possibleBenchmarkNameFilters = new HashSet<string>();
        private readonly HashSet<string> actualFullBenchmarkNames = new HashSet<string>();

        public CorrectionsSuggester(IReadOnlyList<Type> types)
        {
            foreach (var benchmarkRunInfo in TypeFilter.Filter(DefaultConfig.Instance, types))
            {
                foreach (var benchmarkCase in benchmarkRunInfo.BenchmarksCases)
                {
                    string fullBenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase);

                    actualFullBenchmarkNames.Add(fullBenchmarkName);

                    var names = GetAllPartialNames(fullBenchmarkName.Split('.'));
                    possibleBenchmarkNameFilters.AddRange(names);
                }
            }
        }

        public string[] SuggestFor(string userInput)
        {
            if (userInput == null)
                throw new ArgumentNullException(nameof(userInput));

            var calculator = new LevenshteinDistanceCalculator();
            return possibleBenchmarkNameFilters
                .Select(name => (name: name, distance: calculator.Calculate(userInput, name)))
                .Where(tuple => tuple.distance <= PossibleTyposCount)
                .OrderBy(tuple => tuple.distance)
                .ThenBy(tuple => tuple.name)
                .Select(tuple => tuple.name)
                .ToArray();
        }

        public string[] GetAllBenchmarkNames() => actualFullBenchmarkNames.ToArray();

        // A.B.C should get translated into
        // A*
        // A.B*
        // *B*
        // *C
        private static IEnumerable<string> GetAllPartialNames(string[] nameParts)
        {
            for (int partLength = 1; partLength <= nameParts.Length; partLength++)
            {
                for (int i = 0; i < nameParts.Length - partLength + 1; i++)
                {
                    string permutation = string.Join(".", nameParts.Skip(i).Take(partLength));

                    if (i == 0 && partLength == nameParts.Length)
                    {
                        yield return permutation; // we don't want to offer *fullname*
                    }
                    else if (i == 0)
                    {
                        yield return $"{permutation}*";
                    }
                    else
                    {
                        yield return $"*{permutation}*";
                    }
                }
            }
        }
    }
}
