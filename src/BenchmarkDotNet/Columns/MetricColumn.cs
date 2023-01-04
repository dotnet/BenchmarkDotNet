﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Common;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Columns
{
    public class MetricColumn : IColumn
    {
        internal const string UnknownRepresentation = "?";

        // This is used so we won't break the public IMetricDescriptor interface.
        private static readonly HashSet<Type> s_metricsRequiringPositive = new ();

        internal static void RegisterColumnRequiresPositive(IMetricDescriptor metricDescriptor)
        {
            lock (s_metricsRequiringPositive)
            {
                s_metricsRequiringPositive.Add(metricDescriptor.GetType());
            }
        }

        private readonly IMetricDescriptor descriptor;
        private readonly bool force;

        public MetricColumn(IMetricDescriptor metricDescriptor) : this(metricDescriptor, false) { }

        public MetricColumn(IMetricDescriptor metricDescriptor, bool forceShow)
        {
            descriptor = metricDescriptor;
            force = forceShow;
        }

        public string Id => descriptor.Id;
        public string ColumnName => descriptor.DisplayName;
        public string Legend => descriptor.Legend;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Metric;
        public int PriorityInCategory => descriptor.PriorityInCategory;
        public bool IsNumeric => true;
        public UnitType UnitType => descriptor.UnitType;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public bool IsAvailable(Summary summary) => force
            || summary.Reports.Any(report =>
                report.Metrics.TryGetValue(descriptor.Id, out var metric)
                && (!s_metricsRequiringPositive.Contains(metric.Descriptor.GetType()) || metric.Value > 0));

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            if (!summary.HasReport(benchmarkCase) || !summary[benchmarkCase].Metrics.TryGetValue(descriptor.Id, out Metric metric))
                return UnknownRepresentation;
            if (metric.Value == 0.0 && !style.PrintZeroValuesInContent)
                return "-";

            var cultureInfo = summary.GetCultureInfo();

            bool printUnits = style.PrintUnitsInContent || style.PrintUnitsInHeader;
            UnitPresentation unitPresentation = UnitPresentation.FromVisibility(style.PrintUnitsInContent);

            if (printUnits && descriptor.UnitType == UnitType.CodeSize)
                return SizeValue.FromBytes((long) metric.Value).ToString(style.CodeSizeUnit, cultureInfo, descriptor.NumberFormat, unitPresentation);
            if (printUnits && descriptor.UnitType == UnitType.Size)
                return SizeValue.FromBytes((long) metric.Value).ToString(style.SizeUnit, cultureInfo, descriptor.NumberFormat, unitPresentation);
            if (printUnits && descriptor.UnitType == UnitType.Time)
                return TimeInterval.FromNanoseconds(metric.Value).ToString(style.TimeUnit, cultureInfo, descriptor.NumberFormat, unitPresentation);

            return metric.Value.ToString(descriptor.NumberFormat, cultureInfo);
        }

        public override string ToString() => descriptor.DisplayName;
    }
}
