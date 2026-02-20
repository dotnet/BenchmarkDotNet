using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Text;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using System.Globalization;

namespace BenchmarkDotNet.Exporters.OpenMetrics;

public class OpenMetricsExporter : ExporterBase
{
    private const string MetricPrefix = "benchmark_";
    protected override string FileExtension => "metrics";
    protected override string FileCaption => "openmetrics";

    public static readonly IExporter Default = new OpenMetricsExporter();

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        var metricsSet = new HashSet<OpenMetric>();

        foreach (var report in summary.Reports)
        {
            var benchmark = report.BenchmarkCase;
            var gcStats = report.GcStats;
            var descriptor = benchmark.Descriptor;
            var parameters = benchmark.Parameters;

            var stats = report.ResultStatistics;
            var metrics = report.Metrics;
            if (stats == null)
                continue;

            AddCommonMetrics(metricsSet, descriptor, parameters, stats, gcStats);
            AddAdditionalMetrics(metricsSet, metrics, descriptor, parameters);
        }

        WriteMetricsToLogger(logger, metricsSet);
    }

    private static void AddCommonMetrics(HashSet<OpenMetric> metricsSet, Descriptor descriptor, ParameterInstances parameters, Statistics stats, GcStats gcStats)
    {
        metricsSet.AddRange([
            // Mean
            OpenMetric.FromStatistics(
                $"{MetricPrefix}execution_time_nanoseconds",
                "Mean execution time in nanoseconds.",
                "gauge",
                "nanoseconds",
                descriptor,
                parameters,
                stats.Mean),
            // Error
            OpenMetric.FromStatistics(
                $"{MetricPrefix}error_nanoseconds",
                "Standard error of the mean execution time in nanoseconds.",
                "gauge",
                "nanoseconds",
                descriptor,
                parameters,
                stats.StandardError),
            // Standard Deviation
            OpenMetric.FromStatistics(
                $"{MetricPrefix}stddev_nanoseconds",
                "Standard deviation of execution time in nanoseconds.",
                "gauge",
                "nanoseconds",
                descriptor,
                parameters,
                stats.StandardDeviation),
            // GC Stats Gen0 - these are counters, not gauges
            OpenMetric.FromStatistics(
                $"{MetricPrefix}gc_gen0_collections_total",
                "Total number of Gen 0 garbage collections during the benchmark execution.",
                "counter",
                "",
                descriptor,
                parameters,
                gcStats.Gen0Collections),
            // GC Stats Gen1
            OpenMetric.FromStatistics(
                $"{MetricPrefix}gc_gen1_collections_total",
                "Total number of Gen 1 garbage collections during the benchmark execution.",
                "counter",
                "",
                descriptor,
                parameters,
                gcStats.Gen1Collections),
            // GC Stats Gen2
            OpenMetric.FromStatistics(
                $"{MetricPrefix}gc_gen2_collections_total",
                "Total number of Gen 2 garbage collections during the benchmark execution.",
                "counter",
                "",
                descriptor,
                parameters,
                gcStats.Gen2Collections),
            // Total GC Operations
            OpenMetric.FromStatistics(
                $"{MetricPrefix}gc_total_operations_total",
                "Total number of garbage collection operations during the benchmark execution.",
                "counter",
                "",
                descriptor,
                parameters,
                gcStats.TotalOperations),
            // P90 - in nanoseconds
            OpenMetric.FromStatistics(
                $"{MetricPrefix}p90_nanoseconds",
                "90th percentile execution time in nanoseconds.",
                "gauge",
                "nanoseconds",
                descriptor,
                parameters,
                stats.Percentiles.P90),
            // P95 - in nanoseconds
            OpenMetric.FromStatistics(
                $"{MetricPrefix}p95_nanoseconds",
                "95th percentile execution time in nanoseconds.",
                "gauge",
                "nanoseconds",
                descriptor,
                parameters,
                stats.Percentiles.P95)
        ]);
    }

    private static void AddAdditionalMetrics(HashSet<OpenMetric> metricsSet, IReadOnlyDictionary<string, Metric> metrics, Descriptor descriptor, ParameterInstances parameters)
    {
        var reservedMetricNames = new HashSet<string>
        {
            $"{MetricPrefix}execution_time_nanoseconds",
            $"{MetricPrefix}error_nanoseconds",
            $"{MetricPrefix}stddev_nanoseconds",
            $"{MetricPrefix}gc_gen0_collections_total",
            $"{MetricPrefix}gc_gen1_collections_total",
            $"{MetricPrefix}gc_gen2_collections_total",
            $"{MetricPrefix}gc_total_operations_total",
            $"{MetricPrefix}p90_nanoseconds",
            $"{MetricPrefix}p95_nanoseconds"
        };

        foreach (var metric in metrics)
        {
            string metricName = SanitizeMetricName(metric.Key);
            string fullMetricName = $"{MetricPrefix}{metricName}";

            if (reservedMetricNames.Contains(fullMetricName))
                continue;

            metricsSet.Add(OpenMetric.FromMetric(
                fullMetricName,
                metric,
                "gauge", // Assuming all additional metrics are of type "gauge"
                descriptor,
                parameters));
        }
    }

    private static void WriteMetricsToLogger(ILogger logger, HashSet<OpenMetric> metricsSet)
    {
        var emittedHelpType = new HashSet<string>();

        foreach (var metric in metricsSet.OrderBy(m => m.Name))
        {
            if (!emittedHelpType.Contains(metric.Name))
            {
                logger.WriteLine($"# HELP {metric.Name} {metric.Help}");
                logger.WriteLine($"# TYPE {metric.Name} {metric.Type}");
                if (metric.Unit.IsNotBlank())
                {
                    logger.WriteLine($"# UNIT {metric.Name} {metric.Unit}");
                }
                emittedHelpType.Add(metric.Name);
            }

            logger.WriteLine(metric.ToString());
        }

        logger.WriteLine("# EOF");
    }

    private static string SanitizeMetricName(string name)
    {
        var builder = new StringBuilder();
        bool lastWasUnderscore = false;

        foreach (char c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                builder.Append(c);
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore)
            {
                builder.Append('_');
                lastWasUnderscore = true;
            }
        }

        string? result = builder.ToString().Trim('_'); // <-- Trim here

        if (result.Length > 0 && char.IsDigit(result[0]))
            result = "_" + result;

        return result;
    }

    private class OpenMetric : IEquatable<OpenMetric>
    {
        internal string Name { get; }
        internal string Help { get; }
        internal string Type { get; }
        internal string Unit { get; }
        private readonly ImmutableSortedDictionary<string, string> labels;
        private readonly double value;

        private OpenMetric(string name, string help, string type, string unit, ImmutableSortedDictionary<string, string> labels, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Metric name cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Metric type cannot be null or empty.");

            Name = name;
            Help = help;
            Type = type;
            Unit = unit ?? "";
            this.labels = labels ?? throw new ArgumentNullException(nameof(labels));
            this.value = value;
        }

        public static OpenMetric FromStatistics(string name, string help, string type, string unit, Descriptor descriptor, ParameterInstances parameters, double value)
        {
            var labels = BuildLabelDict(descriptor, parameters);
            return new OpenMetric(name, help, type, unit, labels, value);
        }

        public static OpenMetric FromMetric(string fullMetricName, KeyValuePair<string, Metric> metric, string type, Descriptor descriptor, ParameterInstances parameters)
        {
            string help = $"Additional metric {metric.Key}";
            var labels = BuildLabelDict(descriptor, parameters);
            return new OpenMetric(fullMetricName, help, type, "", labels, metric.Value.Value);
        }

        private static readonly Dictionary<string, string> NormalizedLabelKeyCache = [];
        private static string NormalizeLabelKey(string key)
        {
            string normalized = new(key
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            return normalized;
        }

        private static ImmutableSortedDictionary<string, string> BuildLabelDict(Descriptor descriptor, ParameterInstances parameters)
        {
            var dict = new SortedDictionary<string, string>
            {
                ["method"] = descriptor.WorkloadMethod.Name,
                ["type"] = descriptor.TypeInfo
            };
            foreach (var param in parameters.Items)
            {
                string key = NormalizeLabelKey(param.Name);
                string value = EscapeLabelValue(param.Value?.ToString() ?? "");
                dict[key] = value;
            }
            return dict.ToImmutableSortedDictionary();
        }

        private static string EscapeLabelValue(string value)
        {
            return value.Replace("\\", @"\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
        }

        public override bool Equals(object? obj) => Equals(obj as OpenMetric);

        public bool Equals(OpenMetric? other)
        {
            if (other is null)
                return false;

            return Name == other.Name
                && value.Equals(other.value)
                && labels.Count == other.labels.Count
                && labels.All(kv => other.labels.TryGetValue(kv.Key, out string? otherValue) && kv.Value == otherValue);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Name);
            hash.Add(value);

            foreach (var kv in labels)
            {
                hash.Add(kv.Key);
                hash.Add(kv.Value);
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            string labelStr = labels.Count > 0
                ? $"{{{string.Join(", ", labels.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""))}}}"
                : string.Empty;
            return $"{Name}{labelStr} {value.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}