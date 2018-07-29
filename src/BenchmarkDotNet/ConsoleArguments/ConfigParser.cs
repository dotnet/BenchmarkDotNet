using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Toolchains.InProcess;
using CommandLine;

namespace BenchmarkDotNet.ConsoleArguments
{
    public static class ConfigParser
    {
        private const int MinimumDisplayWidth = 80;

        private static readonly IReadOnlyDictionary<string, Job> AvailableJobs = new Dictionary<string, Job>
        {
            { "default", Job.Default },
            { "dry", Job.Dry },
            { "short", Job.ShortRun },
            { "medium", Job.MediumRun },
            { "long", Job.LongRun },
            { "verylong", Job.VeryLongRun }
        };

        private static readonly IReadOnlyDictionary<string, Runtime> AvailableRuntimes = new Dictionary<string, Runtime>
        {
            { "clr", Runtime.Clr },
            { "core", Runtime.Core },
            { "mono", Runtime.Mono },
            { "corert", Runtime.CoreRT }
        };

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        private static readonly IReadOnlyDictionary<string, IExporter[]> AvailableExporters =
            new Dictionary<string, IExporter[]>
            {
                { "csv", new[] { CsvExporter.Default } },
                { "csvmeasurements", new[] { CsvMeasurementsExporter.Default } },
                { "html", new[] { HtmlExporter.Default } },
                { "markdown", new[] { MarkdownExporter.Default } },
                { "atlassian", new[] { MarkdownExporter.Atlassian } },
                { "stackoverflow", new[] { MarkdownExporter.StackOverflow } },
                { "github", new[] { MarkdownExporter.GitHub } },
                { "plain", new[] { PlainExporter.Default } },
                { "rplot", new[] { CsvMeasurementsExporter.Default, RPlotExporter.Default } }, // R Plots depends on having the full measurements available
                { "json", new[] { JsonExporter.Default } },
                { "briefjson", new[] { JsonExporter.Brief } },
                { "fulljson", new[] { JsonExporter.Full } },
                { "asciidoc", new[] { AsciiDocExporter.Default } },
                { "xml", new[] { XmlExporter.Default } },
                { "briefxml", new[] { XmlExporter.Brief } },
                { "fullxml", new[] { XmlExporter.Full } }
            };

        public static (bool isSuccess, ReadOnlyConfig config) Parse(string[] args, ILogger logger)
        {
            (bool isSuccess, ReadOnlyConfig config) result = default;

            using (var parser = CreateParser(logger))
            {
                parser
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsed(options => result = Validate(options, logger) ? (true, CreateConfig(options)) : (false, default))
                    .WithNotParsed(errors => result = (false, default));
            }

            return result;
        }

        private static Parser CreateParser(ILogger logger)
            => new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.EnableDashDash = true;
                settings.IgnoreUnknownArguments = false;
                settings.HelpWriter = new LoggerWrapper(logger);
                settings.MaximumDisplayWidth = Math.Max(MinimumDisplayWidth, GetMaximumDisplayWidth());
            });

        private static bool Validate(CommandLineOptions options, ILogger logger)
        {
            if (!AvailableJobs.ContainsKey(options.BaseJob.ToLowerInvariant()))
            {
                logger.WriteLineError($"The provided base job \"{options.BaseJob}\" is invalid. Available options are: {string.Join(", ", AvailableJobs.Keys)}.");
                return false;
            }

            foreach (string runtime in options.Runtimes)
                if (!AvailableRuntimes.ContainsKey(runtime.ToLowerInvariant()))
                {
                    logger.WriteLineError($"The provided runtime \"{runtime}\" is invalid. Available options are: {string.Join(", ", AvailableRuntimes.Keys)}.");
                    return false;
                }

            foreach (string exporter in options.Exporters)
                if (!AvailableExporters.ContainsKey(exporter.ToLowerInvariant()))
                {
                    logger.WriteLineError($"The provided exporter \"{exporter}\" is invalid. Available options are: {string.Join(", ", AvailableExporters.Keys)}.");
                    return false;
                }

            return true;
        }

        private static ReadOnlyConfig CreateConfig(CommandLineOptions options)
        {
            var config = new ManualConfig();

            config.Add(Expand(GetBaseJob(options), options).ToArray());

            config.Add(options.Exporters.SelectMany(exporter => AvailableExporters[exporter]).ToArray());

            if (options.UseMemoryDiagnoser)
                config.Add(MemoryDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config.Add(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig()));

            if (options.DisplayAllStatistics)
                config.Add(StatisticColumn.AllStatistics);

            if (options.ArtifactsDirectory != null)
                config.ArtifactsPath = options.ArtifactsDirectory.FullName;

            var filters = GetFilters(options).ToArray();
            if (filters.Length > 1)
                config.Add(new UnionFilter(filters));
            else
                config.Add(filters);

            config.SummaryPerType = !options.Join;

            return config.AsReadOnly();
        }

        private static Job GetBaseJob(CommandLineOptions options)
        {
            var baseJob = AvailableJobs[options.BaseJob.ToLowerInvariant()];

            if (baseJob != Job.Dry && options.Outliers != OutlierMode.OnlyUpper)
                baseJob = baseJob.WithOutlierMode(options.Outliers);

            if (options.Affinity.HasValue)
                baseJob = baseJob.WithAffinity((IntPtr) options.Affinity.Value);

            return baseJob;
        }

        private static IEnumerable<Job> Expand(Job baseJob, CommandLineOptions options)
        {
            if (options.RunInProcess)
                yield return baseJob.With(InProcessToolchain.Instance);

            foreach (string runtime in options.Runtimes)
                yield return baseJob.With(AvailableRuntimes[runtime.ToLowerInvariant()]);

            if (!options.RunInProcess && !options.Runtimes.Any() && baseJob != Job.Default)
                yield return baseJob;
        }

        private static IEnumerable<IFilter> GetFilters(CommandLineOptions options)
        {
            if (options.Filters.Any())
                yield return new GlobFilter(options.Filters.ToArray());
            if (options.AllCategories.Any())
                yield return new AllCategoriesFilter(options.AllCategories.ToArray());
            if (options.AnyCategories.Any())
                yield return new AnyCategoriesFilter(options.AnyCategories.ToArray());
            if (options.AttributeNames.Any())
                yield return new AttributesFilter(options.AttributeNames.ToArray());
        }

        private static int GetMaximumDisplayWidth()
        {
            try
            {
                return Console.WindowWidth;
            }
            catch (IOException)
            {
                return MinimumDisplayWidth;
            }
        }
    }
}