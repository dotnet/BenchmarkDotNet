using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public class GcResolver : Resolver
    {
        public static readonly IResolver Instance = new GcResolver();

        private GcResolver()
        {
            Register(GcMode.ServerCharacteristic, () => HostEnvironmentInfo.GetCurrent(RuntimeInformation.Current).IsServerGC);
            Register(GcMode.ConcurrentCharacteristic, () => HostEnvironmentInfo.GetCurrent(RuntimeInformation.Current).IsConcurrentGC);
            Register(GcMode.CpuGroupsCharacteristic, () => false);
            Register(GcMode.ForceCharacteristic, () => true);
            Register(GcMode.AllowVeryLargeObjectsCharacteristic, () => false);
            Register(GcMode.RetainVmCharacteristic, () => false); // Maoni0: "The default is false" https://github.com/dotnet/docs/issues/878#issuecomment-248986456
        }
    }
}