using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using System.Collections.Generic;

namespace BenchmarkDotNet.Configs
{
    public class ConfigParser
    {
        // TODO, refactor this, at the moment they keys for availableOptions and availableParameters MUST stay in sync (which is brittle)
        private static Dictionary<string, Action<ManualConfig, string>> availableOptions = 
            new Dictionary<string, Action<ManualConfig, string>>
            {
                { "jobs", (config, value) => config.Add(ParseJobs(value)) },
                { "columns", (config, value) => config.Add(ParseColumns(value)) },
                { "exporters", (config, value) => { } },
                { "diagnosers", (config, value) => config.Add(ParseDiagnosers(value)) },
                { "analysers", (config, value) => { } },
                { "loggers", (config, value) => { } }
            };
        
        // NOTE: These need to be Lazy<T>, so so that we know they are only called AFTER availableOptions has been initialised (it's static)
        private static Dictionary<string, Lazy<IEnumerable<string>>> availableParameters =
            new Dictionary<string, Lazy<IEnumerable<string>>>
            {
                { "jobs", new Lazy<IEnumerable<string>>(() => availableJobs.Keys) },
                { "columns", new Lazy<IEnumerable<string>>(() => availableColumns.Keys) },
                { "exporters", new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>()) },
                { "diagnosers", new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>()) },
                { "analysers", new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>()) },
                { "loggers", new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>()) }
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
                if (availableOptions.ContainsKey(argument))
                {
                    var action = availableOptions[argument];
                    foreach (var value in values)
                        action(config, value);
                }
                else
                {
                    throw new InvalidOperationException($"\"{split[0]}\" (from \"{arg}\") is an unrecognised Option");
                }
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
            foreach (var option in availableOptions)
            {
                // TODO also consider allowing short version (i.e. '-d' and '--diagnosers')             
                var optionText = $"--{option.Key} <{option.Key.ToUpperInvariant()}>";
                var parameters = string.Empty;
                if (availableParameters.ContainsKey(option.Key))
                    parameters = string.Join(", ", availableParameters[option.Key].Value);
                var explanation = $"Allowed values: {parameters}";
                logger.WriteLineInfo($"  {optionText,-30} {explanation}");
            }
        }

        private static IJob[] ParseJobs(string value)
        {
            if (availableJobs.ContainsKey(value))
                return availableJobs[value];

            throw new InvalidOperationException($"\"{value}\" is an unrecognised Job");
        }

        private static IColumn[] ParseColumns(string value)
        {
            if (availableColumns.ContainsKey(value))
                return availableColumns[value];

            throw new InvalidOperationException($"\"{value}\" is an unrecognised Column");
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