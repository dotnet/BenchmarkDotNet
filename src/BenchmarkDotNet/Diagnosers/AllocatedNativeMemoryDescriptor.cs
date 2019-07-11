﻿using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    internal class AllocatedNativeMemoryDescriptor : IMetricDescriptor
    {
        public string Id => nameof(AllocatedNativeMemoryDescriptor);
        public string DisplayName => $"Allocated native memory";
        public string Legend => $"Allocated native memory per single operation";
        public string NumberFormat => "N0";
        public UnitType UnitType => UnitType.Size;
        public string Unit => SizeUnit.B.Name;
        public bool TheGreaterTheBetter => false;
    }

    internal class NativeMemoryLeakDescriptor : IMetricDescriptor
    {
        public string Id => nameof(NativeMemoryLeakDescriptor);
        public string DisplayName => $"Native memory leak";
        public string Legend => $"Native memory leak size in byte.";
        public string NumberFormat => "N0";
        public UnitType UnitType => UnitType.Size;
        public string Unit => SizeUnit.B.Name;
        public bool TheGreaterTheBetter => false;
    }
}