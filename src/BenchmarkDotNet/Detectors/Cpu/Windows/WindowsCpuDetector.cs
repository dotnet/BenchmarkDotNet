namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class WindowsCpuDetector() : CpuDetector(new PowershellWmiCpuDetector(), new WmicCpuDetector());
