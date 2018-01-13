using System;

namespace BenchmarkDotNet.Jobs
{
    public static class GcModeExtensions
    {
        public static GcMode WithServer(this GcMode mode, bool value) => mode.WithCore(m => m.Server = value);

        public static GcMode WithConcurrent(this GcMode mode, bool value) => mode.WithCore(m => m.Concurrent = value);

        public static GcMode WithCpuGroups(this GcMode mode, bool value) => mode.WithCore(m => m.CpuGroups = value);

        public static GcMode WithForce(this GcMode mode, bool value) => mode.WithCore(m => m.Force = value);

        public static GcMode WithAllowVeryLargeObjects(this GcMode mode, bool value) => mode.WithCore(m => m.AllowVeryLargeObjects = value);

        public static GcMode WithRetainVm(this GcMode mode, bool value) => mode.WithCore(m => m.RetainVm = value);

        public static GcMode WithHeapCount(this GcMode mode, int heapCount) => mode.WithCore(m => m.HeapCount = heapCount);

        public static GcMode WithNoAffinitize(this GcMode mode, bool value) => mode.WithCore(m => m.NoAffinitize = value);

        public static GcMode WithHeapAffinitizeMask(this GcMode mode, int heapAffinitizeMask) => mode.WithCore(m => m.HeapAffinitizeMask = heapAffinitizeMask);

        private static GcMode WithCore(this GcMode mode, Action<GcMode> updateCallback)
        {
            mode = new GcMode().Apply(mode);
            updateCallback(mode);
            return mode;
        }
    }
}