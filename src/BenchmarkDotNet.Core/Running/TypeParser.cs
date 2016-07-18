using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Running
{
    public class TypeParser
    {
        public TypeParser(Type[] types, ILogger logger)
        {
            this.allTypes = types;
            this.logger = logger;
        }

        public class TypeWithMethods
        {
            public Type Type { get; private set; }
            public MethodInfo [] Methods { get; private set; }
            public bool AllMethodsInType { get; private set; }

            public TypeWithMethods(Type type, MethodInfo [] methods = null)
            {
                AllMethodsInType = methods == null;
                Type = type;
                Methods = methods;
            }
        }

        private static Dictionary<string, string> configuration = new Dictionary<string, string>
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
            }
        };

        private Type[] allTypes;
        private readonly ILogger logger;

        internal string[] ReadArgumentList(string[] args)
        {
            while (args.Length == 0)
            {
                PrintAvailable();
                var benchmarkCaptionExample = allTypes.Length == 0 ? "Intro_00" : allTypes.First().Name;
                logger.WriteLineHelp(
                    $"You should select the target benchmark. Please, print a number of a benchmark (e.g. '0') or a benchmark caption (e.g. '{benchmarkCaptionExample}'):");
                var line = Console.ReadLine() ?? "";
                args = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                logger.WriteLine();
            }
            return args;
        }

        internal IEnumerable<TypeWithMethods> MatchingTypesWithMethods(string[] args)
        {
            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = allTypes[i];
                if (args.Any(arg => type.Name.ToLower().StartsWith(arg.ToLower())) || 
                    args.Contains("#" + i) || args.Contains("" + i) || args.Contains("*"))
                {
                    yield return new TypeWithMethods(type);
                }
            }

            var types = new List<Type>();
            var methods = new List<TypeWithMethods>();
            foreach (var arg in args.Where(arg => arg.Contains("=")))
            {
                var split = arg.Split('=');
                var values = split[1].Split(',');
                var argument = split[0].ToLowerInvariant();
                // Allow both "--arg=<value>" and "arg=<value>" (i.e. with and without the double dashes)
                argument = argument.StartsWith(optionPrefix) ? argument.Remove(0, 2) : argument;

                var qualifyingTypes = allTypes.Where(t => t.GetMethods().Any(m => m.HasAttribute<BenchmarkAttribute>()));
                switch (argument)
                {
                    case "method":
                    case "methods":
                        foreach (var type in allTypes)
                        {
                            var allTargetMethods = type.GetMethods().Where(method => method.HasAttribute<BenchmarkAttribute>());
                            var typeWithNamespace = type.Namespace + "." + type.Name;
                            var matchingMethods = allTargetMethods.Where(m => values.Any(v => v == m.Name || v == typeWithNamespace + "." + m.Name)).ToArray();
                            if (matchingMethods.Length > 0)
                            {
                                methods.Add(new TypeWithMethods(type, matchingMethods));
                            }
                        }
                        break;
                    case "class":
                    case "classes":
                        types.AddRange(qualifyingTypes.Where(t => values.Any(v => v == t.Name || v == t.Namespace + "." + t.Name)));
                        break;
                    case "attribute":
                    case "attributes":
                        foreach (var type in allTypes)
                        {
                            // First see if the entire Type/Class has a matching attribute
                            foreach (var attributeName in (type.GetTypeInfo().GetCustomAttributes(true))
                                                              .Select(a => a.GetType().Name))
                            {
                                // Allow short and long version of the attributes to be specified, i.e. "Run" and "RunAttribute"
                                if (values.Any(v => v == attributeName || v + "Attribute" == attributeName))
                                {
                                    types.Add(type);
                                }
                            }

                            // Now see if any methods within the Type/Class have a matching attribute
                            var allTargetMethods = type.GetMethods().Where(method => method.HasAttribute<BenchmarkAttribute>());
                            var matchingMethods = allTargetMethods.Where(m => m.GetCustomAttributes(inherit: false)
                                                                               .Select(a => a.GetType().Name)
                                                                               .Any(a => values.Any(v => v == a || v + "Attribute" == a)))
                                                                  .ToArray();
                            if (matchingMethods.Length > 0)
                            {
                                methods.Add(new TypeWithMethods(type, matchingMethods));
                            }
                        }
                        break;
                    case "namespace":
                    case "namespaces":
                        types.AddRange(qualifyingTypes.Where(t => values.Any(v => v == t.Namespace)));
                        break;
                }
            }

            // Have to normalise, i.e. if we matched some methods in TypeA, but also got "class=TypeA", 
            // we want to use the super-set, i.e. the entire Type, rather than just individual methods
            foreach (var method in methods.Where(m => types.Any(t => t.FullName == m.Type.FullName) == false))
                yield return new TypeWithMethods(method.Type, method.Methods);
            foreach (var type in types)
                yield return new TypeWithMethods(type);
        }

        private string optionPrefix = "--";
        private char[] trimChars = new[] { ' ' };
        private const string breakText = ": ";

        internal void PrintOptions(ILogger logger, int prefixWidth, int outputWidth)
        {
            foreach (var option in configuration)
            {
                var optionText = $"  {optionPrefix}{option.Key} <{option.Key.ToUpperInvariant()}>";
                logger.WriteResult($"{optionText.PadRight(prefixWidth)}");

                var maxWidth = outputWidth - prefixWidth - Environment.NewLine.Length - breakText.Length;
                var lines = StringAndTextExtensions.Wrap(option.Value, maxWidth);
                if (lines.Count == 0)
                {
                    logger.WriteLine();
                    continue;
                }

                logger.WriteLineInfo($"{breakText}{lines.First().Trim(trimChars)}");
                var padding = new string(' ', prefixWidth);
                foreach (var line in lines.Skip(1))
                    logger.WriteLineInfo($"{padding}{breakText}{line.Trim(trimChars)}");
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
            for (int i = 0; i < allTypes.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), allTypes[i].Name));
            logger.WriteLine();
            logger.WriteLine();
        }
    }
}
