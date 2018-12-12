using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.ConsoleArguments
{
    public class CorrectionsSuggester
    {
        // note This is a heuristic value, we suppose that user can make three or fewer typos.
        private static int PossibleTyposCount => 3;
        private readonly HashSet<string> possibleBenchmarkNames = new HashSet<string>();
        private readonly HashSet<string> allBenchmarkNames = new HashSet<string>();

        public CorrectionsSuggester(IReadOnlyList<Type> types)
        {
            var benchmarkNames = new HashSet<string>();
            foreach (var type in types)
            {
                var namesCollection = type.GetMethods()
                    .Where(methodInfo => methodInfo.HasAttribute<BenchmarkAttribute>())
                    .Select(methodInfo => $@"{(type.IsGenericType 
                        ? type.GetDisplayName() 
                        : type.FullName)}.{methodInfo.Name}")
                    .ToArray();
                benchmarkNames.AddRange(namesCollection);
                
                var names = namesCollection.Select(name => name.Split('.', '+')).SelectMany(GetAllPartialNames);
                possibleBenchmarkNames.AddRange(names);
            }

            allBenchmarkNames.AddRange(benchmarkNames.OrderBy(name => name));
        }

        public string[] SuggestFor([NotNull] string userInput)
        {
            if (userInput == null)
                throw new ArgumentNullException(nameof(userInput));
            
            var calculator = new LevenshteinDistanceCalculator();
            return possibleBenchmarkNames
                .Select(name => (name: name, distance: calculator.Calculate(userInput, name)))
                .Where(tuple => tuple.distance <= PossibleTyposCount)
                .OrderBy(tuple => tuple.distance)
                .ThenBy(tuple => tuple.name)
                .Select(tuple => tuple.name)
                .ToArray();
        }

        public string[] GetAllBenchmarkNames() => allBenchmarkNames.ToArray();

        private static IEnumerable<string> GetAllPartialNames(string[] nameParts)
        {
            for (int partLength = 1; partLength <= nameParts.Length; partLength++)
            for (int i = 0; i < nameParts.Length - partLength + 1; i++)
                yield return string.Join(".", nameParts.Skip(i).Take(partLength));
        }
    }
}
