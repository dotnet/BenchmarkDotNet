using System;
using System.Globalization;
using Perfolizer.Horology;
using Pragmastat.Metrology;

namespace BenchmarkDotNet.Helpers;

public static class UnitHelper
{
    /// <summary>
    /// The abbreviation to print for the given unit.
    /// Perfolizer reports ASCII-only abbreviations ("us"), while BenchmarkDotNet prints the Unicode ones ("μs")
    /// and downgrades them via ToAscii for terminals without Unicode support.
    /// </summary>
    public static string GetAbbreviation(this MeasurementUnit unit) =>
        unit == TimeUnit.Microsecond ? AsciiHelper.Mu + "s" : unit.Abbreviation;

    /// <summary>
    /// Formats a measurement the way BenchmarkDotNet presents it:
    /// the nominal value, a gap, and the unit abbreviation.
    /// </summary>
    public static string Format(Measurement measurement, string? format = null,
        IFormatProvider? formatProvider = null, bool printUnit = true)
    {
        string nominalPart = measurement.NominalValue.ToString(format, formatProvider ?? CultureInfo.InvariantCulture);
        string abbreviation = measurement.Unit.GetAbbreviation();
        return printUnit && abbreviation.Length > 0 ? $"{nominalPart} {abbreviation}" : nominalPart;
    }

    /// <summary>
    /// Normalizes user input so that units can be spelled with Unicode abbreviations ("5μs")
    /// even though Perfolizer parsers only know the ASCII ones.
    /// </summary>
    public static string NormalizeUnits(string s) => s.ToAscii();

    public static string ToDefaultString(this TimeInterval timeInterval, string? format = null) =>
        Format(timeInterval.ToMeasurement(), format ?? "0.###");
}
