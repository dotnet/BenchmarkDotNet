using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal class TypeParser
    {
        private const string OptionPrefix = "--";
        private const string BreakText = ": ";

        private static readonly Dictionary<string, string> Configuration = CreateConfiguration();
        private static readonly char[] TrimChars = { ' ' };

        private static bool consoleCancelKeyPressed = false;
        
        private readonly Type[] allTypes;
        private readonly ILogger logger;

        static TypeParser() => Console.CancelKeyPress += (_, __) => consoleCancelKeyPressed = true; 

        internal TypeParser(Type[] types, ILogger logger)
        {
            this.logger = logger;
            allTypes = GenericBenchmarksBuilder.GetRunnableBenchmarks(types);
        }

        internal class TypeWithMethods
        {
            public Type Type { get; }
            public MethodInfo[] Methods { get; }
            public bool AllMethodsInType { get; }

            public TypeWithMethods(Type type, MethodInfo[] methods = null)
            {
                Type = type;
                Methods = methods;
                AllMethodsInType = methods == null;
            }
        }

        internal IEnumerable<TypeWithMethods> GetAll() => allTypes.Select(type => new TypeWithMethods(type));

        internal IEnumerable<TypeWithMethods> AskUser()
        {
            if (allTypes.IsEmpty())
            {
                logger.WriteError("No benchmarks to choose from. Make sure you provided public types with public [Benchmark] methods.");
                return Array.Empty<TypeWithMethods>();
            }

            var selectedTypes = new List<TypeWithMethods>();
            var benchmarkCaptionExample = allTypes.First().GetDisplayName();

            while (selectedTypes.Count == 0  && !consoleCancelKeyPressed)
            {
                PrintAvailable();
                
                if (consoleCancelKeyPressed)
                    break;

                logger.WriteLineHelp($"You should select the target benchmark(s). Please, print a number of a benchmark (e.g. '0') or a contained benchmark caption (e.g. '{benchmarkCaptionExample}'):");
                logger.WriteLineHelp("If you want to select few, please separate them with space ` ` (e.g. `1 2 3`)");

                var userInput = Console.ReadLine() ?? "";

                selectedTypes.AddRange(GetMatching(userInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
                logger.WriteLine();
            }

            return selectedTypes;
        }

        private IEnumerable<TypeWithMethods> GetMatching(string[] userInput)
        {
            if (userInput.IsEmpty())
                yield break;

            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = allTypes[i];

                if (userInput.Any(arg => type.GetDisplayName().ContainsWithIgnoreCase(arg))
                    || userInput.Contains("#" + i)
                    || userInput.Contains("" + i)
                    || userInput.Contains("*"))
                {
                    yield return new TypeWithMethods(type);
                }
            }
        }

        internal void PrintOptions(int prefixWidth, int outputWidth)
        {
            foreach (var option in Configuration)
            {
                var optionText = $"  {OptionPrefix}{option.Key}=<{option.Key.ToUpperInvariant()}>";
                logger.WriteResult($"{optionText.PadRight(prefixWidth)}");

                var maxWidth = outputWidth - prefixWidth - System.Environment.NewLine.Length - BreakText.Length;
                var lines = StringAndTextExtensions.Wrap(option.Value, maxWidth);
                if (lines.Count == 0)
                {
                    logger.WriteLine();
                    continue;
                }

                logger.WriteLineInfo($"{BreakText}{lines.First().Trim(TrimChars)}");
                var padding = new string(' ', prefixWidth);
                foreach (var line in lines.Skip(1))
                    logger.WriteLineInfo($"{padding}{BreakText}{line.Trim(TrimChars)}");
            }
        }

        private void PrintAvailable()
        {
            if (allTypes.IsEmpty())
            {
                logger.WriteLineError("You don't have any benchmarks");
                return;
            }

            logger.WriteLineHelp($"Available Benchmark{(allTypes.Length > 1 ? "s" : "")}:");

            int numberWidth = allTypes.Length.ToString().Length;
            for (int i = 0; i < allTypes.Length && !consoleCancelKeyPressed; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), allTypes[i].GetDisplayName()));

            if (!consoleCancelKeyPressed)
            {
                logger.WriteLine();
                logger.WriteLine();
            }
        }
        
        private static Dictionary<string, string> CreateConfiguration()
        {
            return new Dictionary<string, string>
            {
                {
                    "method",
                    "run a given test method (just the method name, i.e. 'MyTestMethod', or can be fully specified, i.e. 'MyNamespace.MyClass.MyTestMethod')"
                },
                {
                    "class",
                    "run all methods in a given test class (just the class name, i.e. 'MyClass', or can be fully specified, i.e. 'MyNamespace.MyClass')"
                },
                {
                    "namespace",
                    "run all methods in a given namespace (i.e. 'MyNamespace.MySubNamespace')"
                },
                {
                    "attribute",
                    "run all methods with given attribute (applied to class or method)"
                }
            };
        }
    }
}
