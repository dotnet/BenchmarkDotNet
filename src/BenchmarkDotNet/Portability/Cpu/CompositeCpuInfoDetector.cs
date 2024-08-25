using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability.Cpu.Linux;
using BenchmarkDotNet.Portability.Cpu.macOS;
using BenchmarkDotNet.Portability.Cpu.Windows;

namespace BenchmarkDotNet.Portability.Cpu;

internal class CompositeCpuInfoDetector(params ICpuInfoDetector[] detectors) : ICpuInfoDetector
{
    public bool IsApplicable() => detectors.Any(loader => loader.IsApplicable());

    public CpuInfo? Detect() => detectors
        .Where(loader => loader.IsApplicable())
        .Select(loader => loader.Detect())
        .WhereNotNull()
        .FirstOrDefault();
}