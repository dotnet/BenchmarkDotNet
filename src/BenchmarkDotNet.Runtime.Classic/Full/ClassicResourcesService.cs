using System;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Full
{
    internal class ClassicResourcesService : IResourcesService
    {
        public void EnableMonitoring()
        {
            AppDomain.MonitoringIsEnabled = true;
        }

        public long GetAllocatedBytes() => AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
    }
}