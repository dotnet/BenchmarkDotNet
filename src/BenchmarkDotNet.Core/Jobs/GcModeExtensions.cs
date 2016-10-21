using System;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public static class GcModeExtensions
    {
        public static GcMode WithServer(this GcMode mode, bool value)
        {
            mode.Server = value;
            return mode;
        }
        public static GcMode WithConcurrent(this GcMode mode, bool value)
        {
            mode.Concurrent = value;
            return mode;
        }
        public static GcMode WithCpuGroups(this GcMode mode, bool value)
        {
            mode.CpuGroups = value;
            return mode;
        }
        public static GcMode WithForce(this GcMode mode, bool value)
        {
            mode.Force = value;
            return mode;
        }
        public static GcMode WithAllowVeryLargeObjects(this GcMode mode, bool value)
        {
            mode.AllowVeryLargeObjects = value;
            return mode;
        }
    }
}