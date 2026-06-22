namespace BenchmarkDotNet.Diagnosers;

public interface IHardwareCounterProvider
{
    IEnumerable<string> GetVariants(HardwareCounter hardwareCounter);
}