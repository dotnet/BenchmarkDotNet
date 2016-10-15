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
            var envMode = Job.Default.Env;
            Register(envMode.Platform, RuntimeInformation.GetCurrentPlatform);
            Register(envMode.Runtime, RuntimeInformation.GetCurrentRuntime);
            Register(envMode.Jit, RuntimeInformation.GetCurrentJit);
            Register(envMode.Affinity, RuntimeInformation.GetCurrentAffinity);

            // TODO: find a better place
            var acc = Job.Default.Accuracy;
            Register(acc.AnaylyzeLaunchVariance, () => false);
            var run = Job.Default.Run;
            Register(run.UnrollFactor, () => 16);
            Register(run.InvocationCount, () => 1);
        }
    }
}