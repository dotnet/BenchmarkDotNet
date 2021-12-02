using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    internal class NativeCodeSizeMetricDescriptor : IMetricDescriptor
    {
        public string Id => "Native Code Size";
        public string DisplayName => "Code Size";
        public string Legend => "Native code size of the disassembled method(s)";
        public string NumberFormat => "0.##";
        public UnitType UnitType => UnitType.CodeSize;
        public string Unit => SizeUnit.B.Name;
        public bool TheGreaterTheBetter => false;
        public int PriorityInCategory => 0;
    }
}
