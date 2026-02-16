using System.Collections.Generic;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class Node
    {
        public required string Name { get; init; }

        public List<Node> Children { get; } = new List<Node>();
    }
}
