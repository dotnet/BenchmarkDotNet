using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Energy;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnergyDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public EnergyDiagnoserAttribute(EnergyCountersSetup setup = EnergyCountersSetup.Default)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new EnergyDiagnoser(new EnergyDiagnoserConfig(setup)));
        }
    }
}
