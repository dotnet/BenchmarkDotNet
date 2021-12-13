using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Reports
{
    public class Metric
    {
        public double Value { get; }

        public IMetricDescriptor Descriptor { get; }

        public Metric(IMetricDescriptor descriptor, double value)
        {
            Descriptor = descriptor;
            Value = value;
        }
    }

    public interface IMetricDescriptor
    {
        [PublicAPI] string Id { get; }

        [PublicAPI] string DisplayName { get; }

        [PublicAPI] string Legend { get; }

        [PublicAPI] string NumberFormat { get; }

        [PublicAPI] UnitType UnitType { get; }

        [PublicAPI] string Unit { get; }

        [PublicAPI] bool TheGreaterTheBetter { get; }

        [PublicAPI] int PriorityInCategory { get; }
    }

    public class MetricDescriptorEqualityComparer : EqualityComparer<IMetricDescriptor>
    {
        public static readonly EqualityComparer<IMetricDescriptor> Instance = new MetricDescriptorEqualityComparer();

        public override bool Equals(IMetricDescriptor x, IMetricDescriptor y) => x.Id.Equals(y.Id);

        public override int GetHashCode(IMetricDescriptor obj) => obj.Id.GetHashCode();
    }
}