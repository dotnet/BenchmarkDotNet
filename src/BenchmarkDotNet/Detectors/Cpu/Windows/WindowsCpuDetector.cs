namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class WindowsCpuDetector() : CpuDetector(new DefaultCpuDetector(), new PowershellWmiCpuDetector(),
    new WmicCpuDetector());