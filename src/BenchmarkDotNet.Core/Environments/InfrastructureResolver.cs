using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class InfrastructureResolver : Resolver
    {
        public static readonly IResolver Instance = new InfrastructureResolver();

        private InfrastructureResolver()
        {
            var infra = Job.Default.Infrastructure;
            Register(InfrastructureMode.ClockCharacteristic, () => Chronometer.BestClock);
            Register(InfrastructureMode.EngineFactoryCharacteristic, () => new EngineFactory());
            Register(InfrastructureMode.BuildConfigurationCharacteristic, () => InfrastructureMode.ReleaseConfigurationName);

            Register(InfrastructureMode.ArgumentsCharacteristic, Array.Empty<Argument>);
            Register(InfrastructureMode.EnvironmentVariablesCharacteristic, Array.Empty<EnvironmentVariable>);
        }
    }
}