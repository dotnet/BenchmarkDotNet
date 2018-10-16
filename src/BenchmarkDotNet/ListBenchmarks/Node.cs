﻿using System.Collections.Generic;

namespace BenchmarkDotNet.ListBenchmarks
{
    internal class Node
    {
        public string Name { get; set; }

        public List<Node> Children { get; } = new List<Node>();
    }
}
