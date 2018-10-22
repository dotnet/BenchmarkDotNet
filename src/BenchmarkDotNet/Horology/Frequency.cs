using System.Globalization;
using BenchmarkDotNet.Environments;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    [PublicAPI]
    public struct Frequency
    {
        [PublicAPI] public double Hertz { get; }

        [PublicAPI] public Frequency(double hertz) => Hertz = hertz;

        [PublicAPI] public Frequency(double value, FrequencyUnit unit) : this(value * unit.HertzAmount) { }

        [PublicAPI] public static readonly Frequency Zero = new Frequency(0);
        [PublicAPI] public static readonly Frequency Hz = FrequencyUnit.Hz.ToFrequency();
        [PublicAPI] public static readonly Frequency KHz = FrequencyUnit.KHz.ToFrequency();
        [PublicAPI] public static readonly Frequency MHz = FrequencyUnit.MHz.ToFrequency();
        [PublicAPI] public static readonly Frequency GHz = FrequencyUnit.GHz.ToFrequency();

        [PublicAPI, Pure] public TimeInterval ToResolution() => TimeInterval.Second / Hertz;

        [PublicAPI, Pure] public double ToHz() => this / Hz;
        [PublicAPI, Pure] public double ToKHz() => this / KHz;
        [PublicAPI, Pure] public double ToMHz() => this / MHz;
        [PublicAPI, Pure] public double ToGHz() => this / GHz;

        [PublicAPI, Pure] public static Frequency FromHz(double value) => Hz * value;
        [PublicAPI, Pure] public static Frequency FromKHz(double value) => KHz * value;
        [PublicAPI, Pure] public static Frequency FromMHz(double value) => MHz * value;
        [PublicAPI, Pure] public static Frequency FromGHz(double value) => GHz * value;

        [PublicAPI, Pure] public static implicit operator Frequency(double value) => new Frequency(value);
        [PublicAPI, Pure] public static implicit operator double(Frequency property) => property.Hertz;

        [PublicAPI, Pure] public static double operator /(Frequency a, Frequency b) => 1.0 * a.Hertz / b.Hertz;
        [PublicAPI, Pure] public static Frequency operator /(Frequency a, double k) => new Frequency(a.Hertz / k);
        [PublicAPI, Pure] public static Frequency operator /(Frequency a, int k) => new Frequency(a.Hertz / k);
        [PublicAPI, Pure] public static Frequency operator *(Frequency a, double k) => new Frequency(a.Hertz * k);
        [PublicAPI, Pure] public static Frequency operator *(Frequency a, int k) => new Frequency(a.Hertz * k);
        [PublicAPI, Pure] public static Frequency operator *(double k, Frequency a) => new Frequency(a.Hertz * k);
        [PublicAPI, Pure] public static Frequency operator *(int k, Frequency a) => new Frequency(a.Hertz * k);
        
        [PublicAPI, Pure] public static bool TryParse(string s, FrequencyUnit unit, out Frequency freq)
        {
            bool success = double.TryParse(s, NumberStyles.Any, HostEnvironmentInfo.MainCultureInfo, out double result);
            freq = new Frequency(result, unit);
            return success;
        }
        
        [PublicAPI, Pure] public static bool TryParseHz(string s, out Frequency freq) => TryParse(s, FrequencyUnit.Hz, out freq);
        [PublicAPI, Pure] public static bool TryParseKHz(string s, out Frequency freq) => TryParse(s, FrequencyUnit.KHz, out freq);
        [PublicAPI, Pure] public static bool TryParseMHz(string s, out Frequency freq) => TryParse(s, FrequencyUnit.MHz, out freq);
        [PublicAPI, Pure] public static bool TryParseGHz(string s, out Frequency freq) => TryParse(s, FrequencyUnit.GHz, out freq);
        
        [PublicAPI, Pure] public override string ToString() => Hertz + " " + FrequencyUnit.Hz.Name;
    }
}