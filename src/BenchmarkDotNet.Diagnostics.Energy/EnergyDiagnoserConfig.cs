namespace BenchmarkDotNet.Diagnosers
{
    public class EnergyDiagnoserConfig
    {
        public EnergyDiagnoserConfig(EnergyCountersSetup setup = EnergyCountersSetup.Default)
        {
            EnergyCountersSetup = setup;
        }

        public EnergyCountersSetup EnergyCountersSetup { get; }
    }
}
