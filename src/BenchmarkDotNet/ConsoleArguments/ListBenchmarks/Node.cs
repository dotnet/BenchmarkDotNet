using System.Collections.Generic;

#nullable enable

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class Node
    {
        public required string Name { get; init; }

        public List<Node> Children { get; } = new List<Node>();
    }
}
