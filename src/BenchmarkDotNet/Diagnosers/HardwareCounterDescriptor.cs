using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Diagnosers
{
    internal class HardwareCounterDescriptor : IMetricDescriptor
    {
        internal HardwareCounterDescriptor(HardwareCounter hardwareCounter)
        {
            Id = $"{hardwareCounter}";
            DisplayName = $"{hardwareCounter}/Op";
            Legend = $"Hardware counter '{hardwareCounter}' per single operation";
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Legend { get; }

        public string NumberFormat => "N0";

        public UnitType UnitType => UnitType.Dimensionless;

        public string Unit => "Count";

        public bool TheGreaterTheBetter => false;

        public int PriorityInCategory => 0;

        public bool GetIsAvailable(Metric metric) => true;
    }
}
