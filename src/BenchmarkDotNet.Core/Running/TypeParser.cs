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
    internal class TypeParser
    {
        private const string OptionPrefix = "--";
        private const string BreakText = ": ";

        private static readonly Dictionary<string, string> Configuration = CreateConfiguration();
        private static readonly char[] TrimChars = { ' ' };

        private readonly Type[] allTypes;
        private readonly ILogger logger;

        internal TypeParser(Type[] types, ILogger logger)
        {
            allTypes = types.Where(type => type.ContainsRunnableBenchmarks()).ToArray();
            this.logger = logger;
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
            var filters = BuildPredicates(args);

            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = allTypes[i];
                var typeInfo = type.GetTypeInfo();

                if (args.Any(arg => type.Name.ToLower().StartsWith(arg.ToLower()))
                    || args.Contains("#" + i)
                    || args.Contains("" + i)
                    || args.Contains("*"))
                {
                    yield return new TypeWithMethods(type);
                    continue;
                }

                if(filters.areEmpty)
                    continue;

                if (!filters.typePredicates.All(filter => filter(typeInfo)))
                    continue;

                if (filters.methodPredicates.IsEmpty())
                {
                    yield return new TypeWithMethods(type);
                    continue;
                }

                var allBenchmarks = typeInfo.GetBenchmarks();
                var benchmarks = allBenchmarks
                    .Where(method => filters.methodPredicates.All(rule => rule(method)))
                    .ToArray();                               

                if (benchmarks.IsEmpty())
                    continue;

                if(allBenchmarks.Length == benchmarks.Length)
                    yield return new TypeWithMethods(type);
                else
                    yield return new TypeWithMethods(type, benchmarks);
            }
        }

        private (List<Predicate<TypeInfo>> typePredicates, List<Predicate<MethodInfo>> methodPredicates, bool areEmpty) BuildPredicates(string[] args)
        {
            var rules = BuildRules(args);

            var typePredicates = new List<Predicate<TypeInfo>>();
            var methodPredicates = new List<Predicate<MethodInfo>>();

            if (rules.namespaces.Any())
                typePredicates.Add(type => rules.namespaces.Contains(type.Namespace));

            if (rules.classes.Any())
                typePredicates.Add(type => rules.classes.Contains(type.Name) || rules.classes.Contains(type.FullName));

            if (rules.attributes.Any())
            {
                typePredicates.Add(type =>
                {
                    var customTypeAttributes =
                        type.GetCustomAttributes(true)
                            .Select(attribute => attribute.GetType().GetTypeInfo())
                            .ToArray();

                    var customMethodsAttributes =
                        type.GetBenchmarks()
                            .SelectMany(method => method.GetCustomAttributes(true)
                                .Select(attribute => attribute.GetType().GetTypeInfo()))
                            .ToArray();

                    var allCustomAttributes = customTypeAttributes.Union(customMethodsAttributes).Distinct().ToArray();

                    return
                        allCustomAttributes.Any(
                            attribute => rules.attributes.Contains(attribute.Name)
                                         || rules.attributes.Contains(attribute.Name.Replace("Attribute", string.Empty)));
                });

                methodPredicates.Add(method =>
                {
                    var customTypeAttributes =
                        method.DeclaringType.GetTypeInfo().GetCustomAttributes(true)
                            .Select(attribute => attribute.GetType().GetTypeInfo())
                            .ToArray();

                    var customMethodsAttributes = method.GetCustomAttributes(true)
                        .Select(attribute => attribute.GetType().GetTypeInfo())
                        .ToArray();

                    var allCustomAttributes = customTypeAttributes.Union(customMethodsAttributes).Distinct().ToArray();

                    return allCustomAttributes.Any(
                            attribute => rules.attributes.Contains(attribute.Name)
                                         || rules.attributes.Contains(attribute.Name.Replace("Attribute", string.Empty)));
                });
            }

            if (rules.methods.Any())
            {
                methodPredicates.Add(method =>
                    rules.methods.Contains(method.Name)
                    || rules.methods.Contains($"{method.DeclaringType.FullName}.{method.Name}"));
            }

            return (typePredicates, methodPredicates, typePredicates.IsEmpty() && methodPredicates.IsEmpty());
        }

        private (HashSet<string> methods, HashSet<string> classes, HashSet<string> namespaces, HashSet<string> attributes) BuildRules(string[] args)
        {
            var methods = new HashSet<string>();
            var classes = new HashSet<string>();
            var namespaces = new HashSet<string>();
            var attributes = new HashSet<string>();

            foreach (var arg in args.Where(arg => arg.Contains("=")))
            {
                var split = arg.Split('=');
                var values = split[1].Split(',');
                var argument = split[0].ToLowerInvariant();
                // Allow both "--arg=<value>" and "arg=<value>" (i.e. with and without the double dashes)
                argument = argument.StartsWith(OptionPrefix) ? argument.Remove(0, 2) : argument;

                switch (argument)
                {
                    case "method":
                    case "methods":
                        values.ForEach(methodName => methods.Add(methodName));
                        break;
                    case "class":
                    case "classes":
                        values.ForEach(typeName => classes.Add(typeName));
                        break;
                    case "namespace":
                    case "namespaces":
                        values.ForEach(@namespace => namespaces.Add(@namespace));
                        break;
                    case "attribute":
                    case "attributes":
                        values.ForEach(attribute => attributes.Add(attribute));
                        break;
                }
            }

            return (methods, classes, namespaces, attributes);
        }


        internal void PrintOptions(int prefixWidth, int outputWidth)
        {
            foreach (var option in Configuration)
            {
                var optionText = $"  {OptionPrefix}{option.Key} <{option.Key.ToUpperInvariant()}>";
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
            for (int i = 0; i < allTypes.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), allTypes[i].Name));
            logger.WriteLine();
            logger.WriteLine();
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
