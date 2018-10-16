using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.ListBenchmarks {
    internal  class TreeBenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        public void Print(IEnumerable<string> testName)
        {
            List<Node> topLevelNodes = new List<Node>();

            foreach (string test in testName)
            {
                var partsOfName = test.Split('.');
                PrepareNodeTree(topLevelNodes, partsOfName);
            }

            var tree = new AsciiTreeDiagram();
            foreach (var node in topLevelNodes)
            {
                tree.PrintNode(node, indent: "");
            }
        }

        private static void PrepareNodeTree(List<Node> nodes, string[] partsOfName, int index = 0)
        {
            var node = nodes.FirstOrDefault(p => p.Name == partsOfName[index]);
            if (node == null)
            {
                node = new Node { Name = partsOfName[index] };
                nodes.Add(node);
            }

            if (partsOfName.Length > index + 1)
            {
                PrepareNodeTree(node.Children, partsOfName, index + 1);
            }
        }
    }
}