using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public class EnvResolver : Resolver
    {
        public static readonly IResolver Instance = new CompositeResolver(new EnvResolver(), GcResolver.Instance);

        private EnvResolver()
        {
            Register(EnvMode.PlatformCharacteristic, RuntimeInformation.GetCurrentPlatform);
            Register(EnvMode.RuntimeCharacteristic, RuntimeInformation.GetCurrentRuntime);
            Register(EnvMode.JitCharacteristic, RuntimeInformation.GetCurrentJit);
            Register(EnvMode.AffinityCharacteristic, RuntimeInformation.GetCurrentAffinity);

            // TODO: find a better place
            Register(AccuracyMode.AnaylyzeLaunchVarianceCharacteristic, () => false);
            Register(RunMode.UnrollFactorCharacteristic, () => 16);
            Register(RunMode.InvocationCountCharacteristic, () => 1);
        }
    }
}