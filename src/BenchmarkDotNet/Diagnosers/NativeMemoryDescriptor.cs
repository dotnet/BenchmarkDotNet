// It is my Custom file header
// BenchmarkDotNet
// BenchmarkDotNetBenchmarkDotNet
// NativeMemoryDescriptor.cs
// Copyright  - 2019

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    internal class NativeMemoryDescriptor : IMetricDescriptor
    {
        public string Id => nameof(NativeMemoryDescriptor);
        public string DisplayName => $"Total native memory";
        public string Legend => $"Total native memory size in byte";
        public string NumberFormat => "N0";
        public UnitType UnitType => UnitType.Dimensionless;
        public string Unit => "Count";
        public bool TheGreaterTheBetter => false;
    }

    internal class NativeMemoryLeakDescriptor : IMetricDescriptor
    {
        public string Id => nameof(NativeMemoryLeakDescriptor);
        public string DisplayName => $"Native memory leak";
        public string Legend => $"Native memory leak size in byte.";
        public string NumberFormat => "N0";
        public UnitType UnitType => UnitType.Dimensionless;
        public string Unit => "Count";
        public bool TheGreaterTheBetter => false;
    }
}