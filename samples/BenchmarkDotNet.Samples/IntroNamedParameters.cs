using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Samples
{
    public class IntroNamedParameters
    {
        // The source should be an IEnumerable or NamedParameter
        // The underlying type should be the type of your argument or parameter
        public IEnumerable<NamedParameter> Source => new[]
        {
            // Use the WithName extension method to name your arguments or parameters
            Enumerable.Range(1, 3).ToList().WithName("Range (1, 3)"),
            Enumerable.Repeat(1, 3).ToList().WithName("Repeat (1, 3)")
        };

        public IEnumerable<NamedParameter> ParamSource => new[]
        {
            1.WithName("One"),
            2.WithDefaultName() // If you do not wish to specify a name, use WithDefaultName
        };

        // Use [ParamsSource] and/or [ArgumentsSource] as you usually would
        [ParamsSource(nameof(ParamSource))] public int Param { get; set; }

        [Benchmark]
        [ArgumentsSource(nameof(Source))]
        public void IsValid(List<int> argument)
        {
            int toSleep = argument.Sum() - Param;
            Thread.Sleep(toSleep);
        }
    }
}