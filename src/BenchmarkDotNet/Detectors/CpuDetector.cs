using System;
using System.Linq;
using BenchmarkDotNet.Detectors.Cpu;
using BenchmarkDotNet.Detectors.Cpu.Linux;
using BenchmarkDotNet.Detectors.Cpu.macOS;
using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Extensions;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors;

public class CpuDetector(params ICpuDetector[] detectors) : ICpuDetector
{
    public static CpuDetector CrossPlatform => new(
        new WindowsCpuDetector(),
        new LinuxCpuDetector(),
        new MacOsCpuDetector());

    private static readonly Lazy<CpuInfo?> LazyCpu = new(() => CrossPlatform.Detect());
    public static CpuInfo? Cpu => LazyCpu.Value;

    public bool IsApplicable() => detectors.Any(loader => loader.IsApplicable());

    public CpuInfo? Detect() => detectors
        .Where(loader => loader.IsApplicable())
        .Select(loader => loader.Detect())
        .WhereNotNull()
        .FirstOrDefault();
}