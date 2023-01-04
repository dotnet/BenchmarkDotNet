using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;

namespace BenchmarkDotNet.Columns
{
    public static class DefaultColumnProviders
    {
        [PublicAPI] public static readonly IColumnProvider Descriptor = new DescriptorColumnProvider();
        [PublicAPI] public static readonly IColumnProvider Job = new JobColumnProvider();
        [PublicAPI] public static readonly IColumnProvider Statistics = new StatisticsColumnProvider();
        [PublicAPI] public static readonly IColumnProvider Params = new ParamsColumnProvider();
        [PublicAPI] public static readonly IColumnProvider Metrics = new MetricsColumnProvider();

        public static readonly IColumnProvider[] Instance = { Descriptor, Job, Statistics, Params, Metrics };

        private class DescriptorColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary)
            {
                if (summary.BenchmarksCases.Select(b => b.Descriptor.Type.Namespace).Distinct().Count() > 1)
                    yield return TargetMethodColumn.Namespace;
                if (summary.BenchmarksCases.Select(b => b.Descriptor.Type.GetDisplayName()).Distinct().Count() > 1)
                    yield return TargetMethodColumn.Type;
                yield return TargetMethodColumn.Method;
            }
        }

        private class JobColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary) => JobCharacteristicColumn.AllColumns;
        }

        private class StatisticsColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary)
            {
                yield return StatisticColumn.Mean;
                yield return StatisticColumn.Error;

                if (NeedToShow(summary, s => s.Percentiles.P95 > s.Mean + 3 * s.StandardDeviation))
                    yield return StatisticColumn.P95;
                if (NeedToShow(summary, s => s.N >= 3 &&
                                             (!s.GetConfidenceInterval(ConfidenceLevel.L99, s.N).Contains(s.Median) ||
                                              Math.Abs(s.Median - s.Mean) > s.Mean * 0.2)))
                    yield return StatisticColumn.Median;
                if (NeedToShow(summary, s => s.StandardDeviation > 1e-9))
                    yield return StatisticColumn.StdDev;

                if (summary.Reports != null && summary.HasBaselines())
                {
                    yield return BaselineRatioColumn.RatioMean;
                    var stdDevColumn = BaselineRatioColumn.RatioStdDev;
                    var stdDevColumnValues = summary.BenchmarksCases.Select(b => stdDevColumn.GetValue(summary, b));

                    // Hide RatioSD column if values is small
                    // TODO: rewrite and check raw values
                    bool hide = stdDevColumnValues.All(value => value == "0.00" || value == "0.01");
                    if (!hide)
                        yield return BaselineRatioColumn.RatioStdDev;

                    if (HasMemoryDiagnoser(summary))
                    {
                        yield return BaselineAllocationRatioColumn.RatioMean;
                    }
                }
            }

            private static bool NeedToShow(Summary summary, Func<Statistics, bool> check)
            {
                return summary.Reports != null && summary.Reports.Any(r => r.ResultStatistics != null && check(r.ResultStatistics));
            }

            private static bool HasMemoryDiagnoser(Summary summary)
            {
                return summary.BenchmarksCases.Any(c => c.Config.HasMemoryDiagnoser());
            }
        }

        private class ParamsColumnProvider : IColumnProvider
        {
            public IEnumerable<IColumn> GetColumns(Summary summary) => summary
                .BenchmarksCases
                .SelectMany(b => b.Parameters.Items.Select(item => item.Definition))
                .Distinct()
                .Select(definition => new ParamColumn(definition.Name, definition.PriorityInCategory));
        }

        internal class MetricsColumnProvider : IColumnProvider
        {
            // This is used so we don't have to break the public IDiagnoser interface.
            private static readonly Dictionary<Type, HashSet<IMetricDescriptor>> s_forceAddColumns = new ();

            internal static void RegisterForcedColumn(IDiagnoser diagnoser, IMetricDescriptor metricDescriptor)
            {
                var type = diagnoser.GetType();
                if (!s_forceAddColumns.TryGetValue(type, out var set))
                {
                    set = new HashSet<IMetricDescriptor>();
                    s_forceAddColumns.Add(type, set);
                }
                set.Add(metricDescriptor);
            }

            public IEnumerable<IColumn> GetColumns(Summary summary)
            {
                var forcedTypes = new HashSet<Type>();

                return summary
                    .Reports
                    .SelectMany(report =>
                        report.Metrics.Values
                        .Select(metric => metric.Descriptor)
                        // Force add columns in case no measurements were able to be made.
                        .Concat(
                            report.BenchmarkCase.Config.diagnosers
                            .SelectMany(diagnoser =>
                            {
                                if (!s_forceAddColumns.TryGetValue(diagnoser.GetType(), out var descriptors))
                                    return Array.Empty<IMetricDescriptor>();
                                foreach (var desc in descriptors)
                                {
                                    forcedTypes.Add(desc.GetType());
                                }
                                return (IEnumerable<IMetricDescriptor>) descriptors;
                            })
                        )
                    )
                    .Distinct(MetricDescriptorEqualityComparer.Instance)
                    .Select(descriptor => new MetricColumn(descriptor, forcedTypes.Contains(descriptor.GetType())));
            }
        }
    }
}