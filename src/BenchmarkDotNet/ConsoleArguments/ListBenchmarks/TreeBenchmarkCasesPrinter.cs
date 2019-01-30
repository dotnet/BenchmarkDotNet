using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class TreeBenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        // Constants for drawing lines and spaces
        private const string Cross = " ├─";
        private const string Corner = " └─";
        private const string Vertical = " │ ";
        private const string Space = "   ";

        public void Print(IEnumerable<string> testNames, ILogger logger)
        {
            List<Node> topLevelNodes = new List<Node>();

            foreach (string test in testNames)
            {
                var partsOfName = test.Split('.');
                PrepareNodeTree(topLevelNodes, partsOfName);
            }

            foreach (var node in topLevelNodes)
            {
                PrintNode(node, indent: "", logger);
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

        private void PrintNode(Node node, string indent, ILogger logger)
        {
            logger.WriteLine(node.Name);

            // Loop through the children recursively, passing in the
            // indent, and the isLast parameter
            var numberOfChildren = node.Children.Count;
            for (var i = 0; i < numberOfChildren; i++)
            {
                var child = node.Children[i];
                var isLast = (i == (numberOfChildren - 1));

                PrintChildNode(child, indent, isLast, logger);
            }
        }

        private void PrintChildNode(Node node, string indent, bool isLast, ILogger logger)
        {
            logger.Write(indent);

            // Depending if this node is a last child, print the
            // corner or cross, and calculate the indent that will
            // be passed to its children
            if (isLast)
            {
                logger.Write(Corner);
                indent += Space;
            }
            else
            {
                logger.Write(Cross);
                indent += Vertical;
            }

            PrintNode(node, indent, logger);
        }
    }
}