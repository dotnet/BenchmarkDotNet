namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class WindowsCpuDetector() : CpuDetector(
    new DefaultCpuDetector(),
    new SmbiosCpuDetector(),
    new PowershellWmiCpuDetector(),
    new WmicCpuDetector());
