using BenchmarkDotNet.Extensions;
using System.Globalization;

namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Shared display helpers for SMBIOS information classes.
/// Unknown values (missing strings or 0) are rendered as fallback text or "Unknown".
/// </summary>
internal static class SmbiosDisplayExtensions
{
    public static string ToDisplayText(this string? value, string fallback)
    {
        return value.IsBlank()
            ? fallback
            : value.Trim();
    }

    public static string ToDisplaySpeed(this ushort mhz)
    {
        return mhz > 0
            ? $"{mhz.ToString(CultureInfo.InvariantCulture)}MHz"
            : "Unknown";
    }

    public static string ToDisplayMemorySpeed(this uint megatransfersPerSecond)
    {
        return megatransfersPerSecond > 0
            ? $"{megatransfersPerSecond.ToString(CultureInfo.InvariantCulture)} MT/s"
            : "Unknown";
    }

    public static string ToDisplayCount(this int count)
    {
        return count > 0
            ? count.ToString(CultureInfo.InvariantCulture)
            : "Unknown";
    }

    public static string ToDisplayRank(this byte rank)
    {
        return rank > 0
            ? $"Rank {rank.ToString(CultureInfo.InvariantCulture)}"
            : "Rank Unknown";
    }

    public static string ToDisplaySize(this long bytes)
    {
        const long KB = 1024L;
        const long MB = 1024L * 1024;
        const long GB = 1024L * 1024 * 1024;
        if (bytes <= 0)
            return "Unknown";
        if (bytes % GB == 0)
            return $"{bytes / GB} GB";
        if (bytes % MB == 0)
            return $"{bytes / MB} MB";
        if (bytes % KB == 0)
            return $"{bytes / KB} KB";
        return $"{bytes} bytes";
    }

    public static string ToDisplayVoltage(this ushort millivolts)
    {
        if (millivolts == 0)
            return "Unknown";
        return $"{(millivolts / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)} V";
    }
}
