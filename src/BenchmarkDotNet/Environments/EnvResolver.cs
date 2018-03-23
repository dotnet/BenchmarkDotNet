using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
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
            Register(AccuracyMode.AnalyzeLaunchVarianceCharacteristic, () => false);
            Register(RunMode.UnrollFactorCharacteristic, job =>
            {
                // TODO: move it to another place and use the main resolver
                var strategy = job.ResolveValue(RunMode.RunStrategyCharacteristic, RunStrategy.Throughput);
                switch (strategy)
                {
                    case RunStrategy.Throughput:
                        return 16;
                    case RunStrategy.ColdStart:
                    case RunStrategy.Monitoring:
                        return 1;
                    default:
                        throw new NotSupportedException($"Unknown runStrategy: {strategy}");
                }
            });
            Register(RunMode.InvocationCountCharacteristic, () => 1);
        }
    }
}