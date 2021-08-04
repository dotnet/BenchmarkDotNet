using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Tests.Reports
{
    internal sealed class FakeMetricDescriptor : IMetricDescriptor
    {
        public FakeMetricDescriptor(string id, string legend, string numberFormat = null)
        {
            Id = id;
            Legend = legend;
            NumberFormat = numberFormat;
        }

        public string Id { get; }
        public string DisplayName => Id;
        public string Legend { get; }
        public string NumberFormat { get; }
        public UnitType UnitType { get; }
        public string Unit { get; }
        public bool TheGreaterTheBetter { get; }
        public int PriorityInCategory => 0;
    }
}
