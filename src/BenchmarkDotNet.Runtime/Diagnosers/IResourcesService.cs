namespace BenchmarkDotNet.Diagnosers
{
    internal interface IResourcesService
    {
        void EnableMonitoring();

        long GetAllocatedBytes();
    }
}