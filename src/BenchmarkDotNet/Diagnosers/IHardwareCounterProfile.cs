namespace BenchmarkDotNet.Diagnosers;

/// <summary>
/// Hardware counter profile.
/// </summary>
/// <remarks>
/// Use a profile when the counter in the environment under test has a different name or can provide multiple counters with more detailed information.
/// </remarks>
public interface IHardwareCounterProfile
{
    IEnumerable<string> GetVariants(HardwareCounter hardwareCounter);
}