using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Running
{
    public class TypeParser
    {
        public TypeParser(Type[] types, ILogger logger)
        {
            this.types = types;
            this.logger = logger;
        }

        private class TypeOption
        {
            public Action<string> ProcessOption { get; set; } = value => { };
            public string FixedText { get; set; } = string.Empty;
        }

        private static Dictionary<string, TypeOption> configuration = new Dictionary<string, TypeOption>
        {
            { "method", new TypeOption {
                ProcessOption = value => { throw new InvalidOperationException($"{value} is an unrecognised method"); },
                FixedText = "run a given test method (should be fully specified; i.e., 'MyNamespace.MyClass.MyTestMethod')"
            } },
            { "class", new TypeOption {
                ProcessOption = value => { throw new InvalidOperationException($"{value} is an unrecognised class"); },
                FixedText = "run all methods in a given test class (should be fully specified; i.e., 'MyNamespace.MyClass')"
            } },
            { "namespace", new TypeOption {
                ProcessOption = value => { throw new InvalidOperationException($"{value} is an unrecognised namespace"); },
                FixedText = "run all methods in a given namespace (i.e., 'MyNamespace.MySubNamespace')"
            } },
        };

        private Type[] types;
        private readonly ILogger logger;

        internal string[] ReadArgumentList(string[] args)
        {
            while (args.Length == 0)
            {
                PrintAvailable();
                var benchmarkCaptionExample = types.Length == 0 ? "Intro_00" : types.First().Name;
                logger.WriteLineHelp(
                    $"You should select the target benchmark. Please, print a number of a benchmark (e.g. '0') or a benchmark caption (e.g. '{benchmarkCaptionExample}'):");
                var line = Console.ReadLine() ?? "";
                args = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                logger.WriteLine();
            }
            return args;
        }

        internal IEnumerable<Type> MatchingTypes(string[] args)
        {
            // TODO extend this to support Method, Class & Namespace matching, done using configuration (above)
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (args.Any(arg => type.Name.ToLower().StartsWith(arg.ToLower())) || 
                    args.Contains("#" + i) || args.Contains("" + i) || args.Contains("*"))
                {
                    yield return type;
                }
            }
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
                var lines = StringExtensions.Wrap(option.Value.FixedText, maxWidth);
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
            if (types.IsEmpty())
            {
                logger.WriteLineError("You don't have any benchmarks");
                return;
            }

            logger.WriteLineHelp($"Available Benchmark{(types.Length > 1 ? "s" : "")}:");

            int numberWidth = types.Length.ToString().Length;
            for (int i = 0; i < types.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), types[i].Name));
            logger.WriteLine();
            logger.WriteLine();
        }
    }
}
