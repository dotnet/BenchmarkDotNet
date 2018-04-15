using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class GcResolver : Resolver
    {
        public static readonly IResolver Instance = new GcResolver();

        private GcResolver()
        {
            Register(GcMode.ServerCharacteristic, () => HostEnvironmentInfo.GetCurrent().IsServerGC);
            Register(GcMode.ConcurrentCharacteristic, () => HostEnvironmentInfo.GetCurrent().IsConcurrentGC);
            Register(GcMode.CpuGroupsCharacteristic, () => false);
            Register(GcMode.ForceCharacteristic, () => true);
            Register(GcMode.AllowVeryLargeObjectsCharacteristic, () => false);
            Register(GcMode.RetainVmCharacteristic, () => false); // Maoni0: "The default is false" https://github.com/dotnet/docs/issues/878#issuecomment-248986456
            Register(GcMode.NoAffinitizeCharacteristic, () => false); // Maoni0: https://github.com/dotnet/coreclr/pull/6104/commits/d088712003e8d483872754d6b3c72aa2d4443a93
        }
    }
}