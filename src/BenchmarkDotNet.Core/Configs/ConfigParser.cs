using System;
using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Exporters.Xml;

namespace BenchmarkDotNet.Configs
{
    public class ConfigParser
    {
        private class ConfigOption
        {
            public Action<ManualConfig, string> ProcessOption { get; set; } = (config, value) => { };
            public Action<ManualConfig> ProcessAllOptions { get; set; } = (config) => { };
            public Lazy<IEnumerable<string>> GetAllOptions { get; set; } = new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>());
        }

        // NOTE: GetAllOptions needs to be Lazy<T>, because they call static variables (and then the initialisation order is tricky!!)
        private static Dictionary<string, ConfigOption> configuration = new Dictionary<string, ConfigOption>
        {
            { "jobs", new ConfigOption {
                ProcessOption = (config, value) => config.Add(ParseItem("Job", availableJobs, value)),
                ProcessAllOptions = (config) => config.Add(allJobs.Value),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableJobs.Keys)
            } },
            { "columns", new ConfigOption {
                ProcessOption = (config, value) => config.Add(ParseItem("Column", availableColumns, value)),
                ProcessAllOptions = (config) => config.Add(allColumns.Value),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableColumns.Keys)
            } },
            { "exporters", new ConfigOption {
                ProcessOption = (config, value) => config.Add(ParseItem("Exporter", availableExporters, value)),
                ProcessAllOptions = (config) => config.Add(allExporters.Value),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableExporters.Keys)
            } },
            { "diagnosers", new ConfigOption {
                ProcessOption = (config, value) => config.Add(ParseDiagnosers(value)),
                // TODO these 2 should match the lookup in LoadDiagnosers() in DefaultConfig.cs
                GetAllOptions = new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>())
            } },
            { "analysers", new ConfigOption {
                ProcessOption = (config, value) => config.Add(ParseItem("Analyser", availableAnalysers, value)),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableAnalysers.Keys)
            } },
            { "validators", new ConfigOption {
                ProcessOption = (config, value) => config.Add(ParseItem("Validator", availableValidators, value)),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableValidators.Keys)
            } },
            { "loggers", new ConfigOption {
                // TODO does it make sense to allows Loggers to be configured on the cmd-line?
                ProcessOption = (config, value) => { throw new InvalidOperationException($"{value} is an unrecognised Logger"); },
            } },
        };

        private static Dictionary<string, Job[]> availableJobs =
            new Dictionary<string, Job[]>
            {
                { "default", new [] { Job.Default } },
                { "legacyjitx86", new[] { Job.LegacyJitX86 } },
                { "legacyjitx64", new[] { Job.LegacyJitX64 } },
                { "ryujitx64", new[] { Job.RyuJitX64 } },
                { "ryujitx86", new[] { Job.RyuJitX86 } },
                { "dry", new[] { Job.Dry } },
                { "clr", new[] { Job.Clr } },
                { "mono", new[] { Job.Mono } },
                { "longrun", new[] { Job.LongRun } }
            };
        private static Lazy<Job[]> allJobs = new Lazy<Job[]>(() => availableJobs.SelectMany(e => e.Value).ToArray());

        private static Dictionary<string, IColumn[]> availableColumns =
            new Dictionary<string, IColumn[]>
            {
                { "mean", new [] { StatisticColumn.Mean } },
                { "stderror", new[] { StatisticColumn.StdErr } },
                { "stddev", new[] { StatisticColumn.StdDev } },
                { "operationpersecond", new [] { StatisticColumn.OperationsPerSecond } },
                { "min", new[] { StatisticColumn.Min } },
                { "q1", new[] { StatisticColumn.Q1 } },
                { "median", new[] { StatisticColumn.Median } },
                { "q3", new[] { StatisticColumn.Q3 } },
                { "max", new[] { StatisticColumn.Max } },
                { "allstatistics", StatisticColumn.AllStatistics  },
                { "rank", new[] { RankColumn.Arabic } }
            };
        private static Lazy<IColumn[]> allColumns = new Lazy<IColumn[]>(() => availableColumns.SelectMany(e => e.Value).ToArray());

        private static Dictionary<string, IExporter[]> availableExporters =
            new Dictionary<string, IExporter[]>
            {
                { "csv", new [] { CsvExporter.Default } },
                { "csvmeasurements", new[] { CsvMeasurementsExporter.Default } },
                { "html", new[] { HtmlExporter.Default } },
                { "markdown", new [] { MarkdownExporter.Default } },
                { "atlassian", new[] { MarkdownExporter.Atlassian } },
                { "stackoverflow", new[] { MarkdownExporter.StackOverflow } },
                { "github", new[] { MarkdownExporter.GitHub } },
                { "plain", new[] { PlainExporter.Default } },
                { "rplot", new[] { RPlotExporter.Default } },
                { "json", new[] { JsonExporter.Default } },
                { "briefjson", new[] { JsonExporter.Brief } },
                { "fulljson", new[] { JsonExporter.Full } },
                { "asciidoc", new[] { AsciiDocExporter.Default } },
                { "xml", new[] { XmlExporter.Default } },
                { "briefxml", new[] { XmlExporter.Brief } },
                { "fullxml", new[] { XmlExporter.Full } },
            };
        private static Lazy<IExporter[]> allExporters = new Lazy<IExporter[]>(() => availableExporters.SelectMany(e => e.Value).ToArray());

        private static Dictionary<string, IAnalyser[]> availableAnalysers =
            new Dictionary<string, IAnalyser[]>
            {
                { "environment", new [] { EnvironmentAnalyser.Default } }
            };

        private static Dictionary<string, IValidator[]> availableValidators =
            new Dictionary<string, IValidator[]>
            {
                { "baseline", new [] { BaselineValidator.FailOnError } },
                { "jitOptimizations", new [] { JitOptimizationsValidator.DontFailOnError } },
                { "jitOptimizationsFailOnError", new [] { JitOptimizationsValidator.FailOnError } },
            };

        public IConfig Parse(string[] args)
        {
            var config = new ManualConfig();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i]
                    .ToLowerInvariant() // normalize
                    .Replace(optionPrefix, string.Empty); // Allow both "--arg=<value>" and "arg=<value>" (i.e. with and without the double dashes)

                var containsEqualitySign = arg.Contains('=');
                if (!containsEqualitySign && !arg.EndsWith("s"))
                    arg += "s"; // make it plural

                bool isArgument = containsEqualitySign 
                    || (configuration.ContainsKey(arg) && i + 1 < args.Length); // make sure we know it and there is next value

                if(!isArgument)
                    continue;

                var argumentName = containsEqualitySign ? arg.Split('=')[0] : arg;
                var values = (containsEqualitySign ? arg.Split('=')[1] : args[++i]).Split(',');

                // Delibrately allow both "job" and "jobs" to be specified, makes it easier for users!!
                var argument = argumentName.EndsWith("s") ? argumentName : argumentName + "s";
                
                argument = argument.StartsWith(optionPrefix) ? argument.Remove(0, 2) : argument;

                switch (argument)
                {
                    case "categorys": // for now all the argument names at the place end with "s"
                    case "allcategories":
                        config.Add(new AllCategoriesFilter(values));
                        break;
                    case "anycategories":
                        config.Add(new AnyCategoriesFilter(values));
                        break;
                }

                if (configuration.ContainsKey(argument) == false)
                    continue;

                if (values.Length == 1 && values[0] == "all")
                {
                    configuration[argument].ProcessAllOptions(config);
                }
                else
                {
                    var processOption = configuration[argument].ProcessOption;
                    foreach (var value in values)
                        processOption(config, value);
                }
            }
            return config;
        }

        // TODO also consider allowing short version (i.e. '-d' and '--diagnosers')
        private string optionPrefix = "--";
        private char[] trimChars = new[] { ' ' };
        private const string breakText = ": ";

        public void PrintOptions(ILogger logger, int prefixWidth, int outputWidth)
        {
            foreach (var option in configuration)
            {
                var optionText = $"  {optionPrefix}{option.Key} <{option.Key.ToUpperInvariant()}>";
                logger.WriteResult($"{optionText.PadRight(prefixWidth)}");

                var parameters = string.Join(", ", option.Value.GetAllOptions.Value);
                var explanation = $"Allowed values: ";
                logger.WriteInfo($": {explanation}");

                var maxWidth = outputWidth - prefixWidth - explanation.Length - System.Environment.NewLine.Length - breakText.Length;
                var lines = StringAndTextExtensions.Wrap(parameters, maxWidth);
                if (lines.Count == 0)
                {
                    logger.WriteLine();
                    continue;
                }

                logger.WriteLineInfo($"{lines.First().Trim(trimChars)}");
                var padding = new string(' ', prefixWidth);
                var explanationPadding = new string(' ', explanation.Length);
                foreach (var line in lines.Skip(1))
                    logger.WriteLineInfo($"{padding}{breakText}{explanationPadding}{line.Trim(trimChars)}");
            }
        }

        private static T[] ParseItem<T>(string itemName, Dictionary<string, T[]> itemLookup, string value)
        {
            if (itemLookup.ContainsKey(value))
                return itemLookup[value];

            throw new InvalidOperationException($"\"{value}\" is an unrecognised {itemName}");
        }

        private static IDiagnoser[] ParseDiagnosers(string value)
        {
            foreach (var diagnoser in DiagnosersLoader.LazyLoadedDiagnosers.Value)
            {
                if (value == diagnoser.GetType().Name.Replace("Diagnoser", "").ToLowerInvariant())
                    return new[] { diagnoser };
            }
            throw new InvalidOperationException($"{value} is an unrecognised Diagnoser");
        }
    }
}