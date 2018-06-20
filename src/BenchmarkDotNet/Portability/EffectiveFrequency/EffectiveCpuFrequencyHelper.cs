using BenchmarkDotNet.Portability.Cpu;

namespace BenchmarkDotNet.Portability.EffectiveFrequency
{
    /// <summary>Provides advanced CPU information</summary>
    public class EffectiveCpuFrequencyProvider
    {
        internal static CpuInfo GetEffectiveCpuInfo()
        {
            if (RuntimeInformation.IsWindows())
                return WindowsEffectiveCpuProvider.WindowsEffectiveCpuInfo.Value;
            if (RuntimeInformation.IsLinux())
                return LinuxEffectiveCpuProvider.LinuxEffectiveCpuInfo.Value;
            if (RuntimeInformation.IsMacOSX())
                return MacEffectiveCpuProvider.MacEffectiveCpuInfo.Value;

            return null;
        }
    }
}