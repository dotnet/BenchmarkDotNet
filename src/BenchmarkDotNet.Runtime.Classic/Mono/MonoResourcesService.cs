using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Mono
{
    // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-
    internal class MonoResourcesService : IResourcesService
    {
        public void EnableMonitoring()
        {
        }

        public long GetAllocatedBytes() => default(long);
    }
}