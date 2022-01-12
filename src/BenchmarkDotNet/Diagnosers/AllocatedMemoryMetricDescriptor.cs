using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    internal class AllocatedMemoryMetricDescriptor : IMetricDescriptor
    {
        internal static readonly IMetricDescriptor Instance = new AllocatedMemoryMetricDescriptor();

        public string Id => "Allocated Memory";
        public string DisplayName => Column.Allocated;
        public string Legend => "Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)";
        public string NumberFormat => "0.##";
        public UnitType UnitType => UnitType.Size;
        public string Unit => SizeUnit.B.Name;
        public bool TheGreaterTheBetter => false;
        public int PriorityInCategory => GC.MaxGeneration + 1;
    }
}