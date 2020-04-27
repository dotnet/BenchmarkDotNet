using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Environments
{
    public class InfrastructureResolver : Resolver
    {
        public static readonly IResolver Instance = new InfrastructureResolver();

        private InfrastructureResolver()
        {
            Register(InfrastructureMode.ClockCharacteristic, () => Chronometer.BestClock);
            Register(InfrastructureMode.EngineFactoryCharacteristic, () => new EngineFactory());
            Register(InfrastructureMode.BuildConfigurationCharacteristic, () => InfrastructureMode.ReleaseConfigurationName);

            Register(InfrastructureMode.ArgumentsCharacteristic, Array.Empty<Argument>);
            Register(InfrastructureMode.NuGetReferencesCharacteristic, Array.Empty<NuGetReference>);
        }
    }
}