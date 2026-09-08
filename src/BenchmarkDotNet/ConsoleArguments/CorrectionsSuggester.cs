using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.ConsoleArguments
{
    internal sealed class CorrectionsSuggester
    {
        // note This is a heuristic value, we suppose that user can make three or fewer typos.
        private static int PossibleTyposCount => 3;
        private readonly HashSet<string> possibleBenchmarkNameFilters = [];
        private readonly HashSet<string> actualFullBenchmarkNames = [];

        internal CorrectionsSuggester(IReadOnlyList<Type> types)
            => Populate(TypeFilter.Filter(DefaultConfig.Instance, types));

        private CorrectionsSuggester(BenchmarkRunInfo[] benchmarkRunInfos)
            => Populate(benchmarkRunInfos);

        internal static async ValueTask<CorrectionsSuggester> CreateAsync(IReadOnlyList<Type> types, CancellationToken cancellationToken = default)
            => new(await TypeFilter.FilterAsync(DefaultConfig.Instance, types, cancellationToken).ConfigureAwait());

        private void Populate(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            foreach (var benchmarkRunInfo in benchmarkRunInfos)
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
            ArgumentNullException.ThrowIfNull(userInput);

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
