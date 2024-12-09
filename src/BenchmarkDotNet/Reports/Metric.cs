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

        [PublicAPI] bool GetIsAvailable(Metric metric);
    }

    public interface IMetricDescriptorConfigurationHandler<TConfig>
    {
        void SetConfiguration(TConfig config);
    }

    public class MetricDescriptorConfigurationHandler<TConfig> : IMetricDescriptorConfigurationHandler<TConfig>
    {
        private TConfig _config;

        protected TConfig Config => _config;

        public virtual void SetConfiguration(TConfig config)
        {
            _config = config;
        }
    }

    public abstract class MetricDescriptorSingletonBase<T> where T : class, new()
    {
        private static T _instance;

        protected MetricDescriptorSingletonBase() { }

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }
    }

    public class MetricDescriptorEqualityComparer : EqualityComparer<IMetricDescriptor>
    {
        public static readonly EqualityComparer<IMetricDescriptor> Instance = new MetricDescriptorEqualityComparer();

        public override bool Equals(IMetricDescriptor x, IMetricDescriptor y) => x.Id.Equals(y.Id);

        public override int GetHashCode(IMetricDescriptor obj) => obj.Id.GetHashCode();
    }
}