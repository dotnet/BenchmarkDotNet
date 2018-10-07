using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class IntroCustomEnvironmentInfo
    {
        [CustomEnvironmentInfo]
        public static string CustomLine() => "Single line";

        [CustomEnvironmentInfo]
        public static IEnumerable<string> SequenceOfCustomLines()
        {
            yield return "First line from sequence";
            yield return "Second line from sequence";
        }

        [CustomEnvironmentInfo]
        public static string[] ArrayOfCustomLines() => 
            new[] {
                "First line from array",
                "Second line from array"
            };

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}
