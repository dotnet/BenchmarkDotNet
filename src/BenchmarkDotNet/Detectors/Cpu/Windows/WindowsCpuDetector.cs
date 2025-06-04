namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class WindowsCpuDetector() : CpuDetector(new MosCpuDetector(),
    new WmiLightCpuDetector(), new WmicCpuDetector());