using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace BenchmarkDotNet.Helpers;

public static class UnitHelper
{
    public static readonly UnitPresentation DefaultPresentation = new (true, 0, gap: true);

    public static string ToDefaultString(this TimeInterval timeInterval, string? format = null) => timeInterval.ToString(format, null, DefaultPresentation);
}