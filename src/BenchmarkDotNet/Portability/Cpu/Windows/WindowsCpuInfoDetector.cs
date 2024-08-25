namespace BenchmarkDotNet.Portability.Cpu.Windows;

internal class WindowsCpuInfoDetector() : CompositeCpuInfoDetector(new MosCpuInfoDetector(), new WmicCpuInfoDetector());