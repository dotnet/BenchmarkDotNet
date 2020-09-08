using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Environments
{
    public class EnvironmentResolver : Resolver
    {
        public const int DefaultUnrollFactorForThroughput = 16;

        public static IResolver Instance = new CompositeResolver(new EnvironmentResolver(RuntimeInformationWrapperProvider.RuntimeInformationWrapper), GcResolver.Instance);

        private EnvironmentResolver(IRuntimeInformationWrapper runtimeInformationWrapper)
        {
            Register(EnvironmentMode.PlatformCharacteristic, runtimeInformationWrapper.GetCurrentPlatform);
            Register(EnvironmentMode.RuntimeCharacteristic, runtimeInformationWrapper.GetCurrentRuntime);
            Register(EnvironmentMode.JitCharacteristic, runtimeInformationWrapper.GetCurrentJit);
            Register(EnvironmentMode.AffinityCharacteristic, runtimeInformationWrapper.GetCurrentAffinity);
            Register(EnvironmentMode.EnvironmentVariablesCharacteristic, Array.Empty<EnvironmentVariable>);
            Register(EnvironmentMode.PowerPlanModeCharacteristic, () => PowerManagementApplier.Map(PowerPlan.HighPerformance));

            // TODO: find a better place
            Register(AccuracyMode.AnalyzeLaunchVarianceCharacteristic, () => false);
            Register(RunMode.UnrollFactorCharacteristic, job =>
            {
                // TODO: move it to another place and use the main resolver
                var strategy = job.ResolveValue(RunMode.RunStrategyCharacteristic, RunStrategy.Throughput);
                switch (strategy)
                {
                    case RunStrategy.Throughput:
                        return DefaultUnrollFactorForThroughput;
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