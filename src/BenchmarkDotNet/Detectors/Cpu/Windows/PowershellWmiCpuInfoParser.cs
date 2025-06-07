using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal static class PowershellWmiCpuInfoParser
{
    internal static CpuInfo? Parse(string? powershellWmiOutput)
    {
}}