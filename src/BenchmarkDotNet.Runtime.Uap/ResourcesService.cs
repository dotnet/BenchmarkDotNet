using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet
{
    // todo: make sure we can't get this infor for UAP
    internal class ResourcesService : IResourcesService
    {
        public void EnableMonitoring()
        {
        }

        public long GetAllocatedBytes() => default(long);
    }
}