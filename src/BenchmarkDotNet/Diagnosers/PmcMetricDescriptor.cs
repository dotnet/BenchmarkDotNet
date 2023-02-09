using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    internal class PmcMetricDescriptor : IMetricDescriptor
    {
        internal PmcMetricDescriptor(PreciseMachineCounter counter)
        {
            Id = counter.Name;
            DisplayName = $"{counter.Name}/Op";
            Legend = $"Hardware counter '{counter.Name}' per single operation";
            TheGreaterTheBetter = counter.Counter.TheGreaterTheBetter();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Legend { get; }
        public bool TheGreaterTheBetter { get; }
        public string NumberFormat => "N0";
        public UnitType UnitType => UnitType.Dimensionless;
        public string Unit => "Count";
        public int PriorityInCategory => 0;
        public bool GetIsAvailable(Metric metric) => true;
    }
}