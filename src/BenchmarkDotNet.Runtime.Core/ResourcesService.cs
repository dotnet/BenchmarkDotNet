using System;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet
{
    internal class ResourcesService : IResourcesService
    {
        private static readonly Func<long> getAllocatedBytesForCurrentThread = GetAllocatedBytesForCurrentThread();

        public void EnableMonitoring()
        {
            // empty on purpose!
        }

        public long GetAllocatedBytes() => getAllocatedBytesForCurrentThread.Invoke();

        private static Func<long> GetAllocatedBytesForCurrentThread()
        {
            // this method is not a part of .NET Standard, so it's not in the contracts
            // but it's implemented so we need reflection hack to get it working
            var methodInfo = typeof(GC).GetAllMethods().SingleOrDefault(method => method.IsStatic && method.Name == "GetAllocatedBytesForCurrentThread");

            return () => (long)methodInfo.Invoke(null, null);
        }
    }
}