using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class GcResolver : Resolver
    {
        public static readonly IResolver Instance = new GcResolver();

        private GcResolver()
        {
            var gc = Job.Default.Env.Gc;
            Register(gc.Server, () => HostEnvironmentInfo.GetCurrent().IsServerGC);
            Register(gc.Concurrent, () => HostEnvironmentInfo.GetCurrent().IsConcurrentGC);
            Register(gc.CpuGroups, () => false);
            Register(gc.Force, () => true);
            Register(gc.AllowVeryLargeObjects, () => false);
        }
    }
}