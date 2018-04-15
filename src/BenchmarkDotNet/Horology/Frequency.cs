using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public struct Frequency
    {
        public double Hertz { get; }

        public Frequency(double hertz) => Hertz = hertz;

        public Frequency(double value, FrequencyUnit unit) : this(value * unit.HertzAmount) { }

        public static readonly Frequency Hz = FrequencyUnit.Hz.ToFrequency();
        public static readonly Frequency KHz = FrequencyUnit.KHz.ToFrequency();
        public static readonly Frequency MHz = FrequencyUnit.MHz.ToFrequency();
        public static readonly Frequency GHz = FrequencyUnit.GHz.ToFrequency();

        [Pure] public TimeInterval ToResolution() => TimeInterval.Second / Hertz;

        [Pure] public double ToHz() => this / Hz;
        [Pure] public double ToKHz() => this / KHz;
        [Pure] public double ToMHz() => this / MHz;
        [Pure] public double ToGHz() => this / GHz;

        [Pure] public static Frequency FromHz(double value) => Hz * value;
        [Pure] public static Frequency FromKHz(double value) => KHz * value;
        [Pure] public static Frequency FromMHz(double value) => MHz * value;
        [Pure] public static Frequency FromGHz(double value) => GHz * value;

        [Pure] public static implicit operator Frequency(double value) => new Frequency(value);
        [Pure] public static implicit operator double(Frequency property) => property.Hertz;

        [Pure] public static double operator /(Frequency a, Frequency b) => 1.0 * a.Hertz / b.Hertz;
        [Pure] public static Frequency operator /(Frequency a, double k) => new Frequency(a.Hertz / k);
        [Pure] public static Frequency operator /(Frequency a, int k) => new Frequency(a.Hertz / k);
        [Pure] public static Frequency operator *(Frequency a, double k) => new Frequency(a.Hertz * k);
        [Pure] public static Frequency operator *(Frequency a, int k) => new Frequency(a.Hertz * k);
        [Pure] public static Frequency operator *(double k, Frequency a) => new Frequency(a.Hertz * k);
        [Pure] public static Frequency operator *(int k, Frequency a) => new Frequency(a.Hertz * k);

        [Pure] public override string ToString() => Hertz + " " + FrequencyUnit.Hz.Name;
    }
}