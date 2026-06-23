namespace BenchmarkDotNet.Diagnosers;

/// <summary>
/// Предоставляет доступ к настройке профиля счетчика,
/// т.к. счетчик может иметь различное именование в зависимости от модели CPU или может иметь различные версии с более детальной информацией.
/// </summary>
public interface IHardwareCounterProfile
{
    IEnumerable<string> GetVariants(HardwareCounter hardwareCounter);
}