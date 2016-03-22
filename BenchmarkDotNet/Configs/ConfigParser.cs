using System;
using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;

namespace BenchmarkDotNet.Configs
{
    public class ConfigParser
    {
        private class Options
        {
            public Action<ManualConfig, string> ProcessOption { get; set; } = (config, value) => { };
            public Lazy<IEnumerable<string>> GetAllOptions { get; set; } = new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>());
        }

        // NOTE: GetAllOptions needs to be Lazy<T>, because they call static variables (and initialisation order it tricky!!)
        private static Dictionary<string, Options> configuration = new Dictionary<string, Options>
        {
            { "jobs", new Options {
                ProcessOption = (config, value) => config.Add(ParseItem("Job", availableJobs, value)),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableJobs.Keys)
            } },
            { "columns", new Options {
                ProcessOption = (config, value) => config.Add(ParseItem("Column", availableColumns, value)),
                GetAllOptions = new Lazy<IEnumerable<string>>(() => availableColumns.Keys)
            } },
            { "exporters", new Options {
                // TODO allow Exporters to be configured on the cmd line
                ProcessOption = (config, value) => { throw new InvalidOperationException($"{value} is an unrecognised Exporter"); },
            } },
            { "diagnosers", new Options {
                ProcessOption = (config, value) => config.Add(ParseDiagnosers(value)),
                // TODO these 2 should match the lookup in LoadDiagnosers() in DefaultConfig.cs
                GetAllOptions = new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>())
            } },
            { "analysers", new Options {
                // TODO allow Analysers to be configured on the cmd line
                ProcessOption = (config, value) => { throw new InvalidOperationException($"{value} is an unrecognised Analyser"); },
            } },
            { "loggers", new Options {
                // TODO does it make sense to allows Loggers to be configured on the cmd-line?
                ProcessOption = (config, value) => { throw new InvalidOperationException($"{value} is an unrecognised Logger"); },
            } },
        };

        private static Dictionary<string, IJob[]> availableJobs =
            new Dictionary<string, IJob[]>
            {
                { "default", new [] { Job.Default } },
                { "legacyjitx86", new[] { Job.LegacyJitX86 } },
                { "legacyjitx64", new[] { Job.LegacyJitX64 } } ,
                { "ryujitx64", new[] { Job.RyuJitX64 } },
                { "dry", new[] { Job.Dry } },
                { "alljits", Job.AllJits },
                { "clr", new[] { Job.Clr } },
                { "mono", new[] { Job.Mono } },
                { "longrun", new[] { Job.LongRun } }
            };

        private static Dictionary<string, IColumn[]> availableColumns =
            new Dictionary<string, IColumn[]>
            {
                { "mean", new [] { StatisticColumn.Mean } },
                { "stderror", new[] { StatisticColumn.StdError } },
                { "stddev", new[] { StatisticColumn.StdDev } },
                { "operationpersecond", new [] { StatisticColumn.OperationsPerSecond } },
                { "min", new[] { StatisticColumn.Min } },
                { "q1", new[] { StatisticColumn.Q1 } },
                { "median", new[] { StatisticColumn.Median } },
                { "q3", new[] { StatisticColumn.Q3 } },
                { "max", new[] { StatisticColumn.Max } },
                { "allstatistics", StatisticColumn.AllStatistics  },
                { "place", new[] { PlaceColumn.ArabicNumber } }
            };

        public IConfig Parse(string[] args)
        {
            var config = new ManualConfig();
            foreach (var arg in args.Where(arg => arg.Contains("=")))
            {
                var split = arg.ToLowerInvariant().Split('=');
                var values = split[1].Split(',');
                // Delibrately allow both "jobs" and "job" to be specified, makes it easier for users!!
                var argument = split[0].EndsWith("s") ? split[0] : split[0] + "s";

                if (configuration.ContainsKey(argument) == false)
                    throw new InvalidOperationException($"\"{split[0]}\" (from \"{arg}\") is an unrecognised Option");

                var action = configuration[argument].ProcessOption;
                    foreach (var value in values)
                        action(config, value);
            }
            return config;
        }

        public bool ShouldDisplayOptions(string[] args)
        {
            return args.Select(a => a.ToLowerInvariant()).Any(a => a == "--help" || a == "-h");
        }

        public void PrintOptions(ILogger logger)
        {
            logger.WriteLineHeader($"{BenchmarkDotNetInfo.FullTitle}");
            logger.WriteLine();
            logger.WriteLineHeader("Options:");
            foreach (var option in configuration)
            {
                // TODO also consider allowing short version (i.e. '-d' and '--diagnosers')             
                var optionText = $"--{option.Key} <{option.Key.ToUpperInvariant()}>";
                var parameters = string.Join(", ", option.Value.GetAllOptions.Value);
                var explanation = $"Allowed values: {parameters}";
                logger.WriteLineInfo($"  {optionText,-30} {explanation}");
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
            foreach (var diagnoser in DefaultConfig.LazyLoadedDiagnosers.Value)
            {
                if (value == diagnoser.GetType().Name.Replace("Diagnoser", "").ToLowerInvariant())
                    return new[] { diagnoser };
            }
            throw new InvalidOperationException($"{value} is an unrecognised Diagnoser");
        }
    }
}