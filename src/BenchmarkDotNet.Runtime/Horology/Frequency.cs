using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public struct Frequency
    {
        public double Hertz { get; }

        public Frequency(double hertz)
        {
            Hertz = hertz;
        }

        public Frequency(double value, FrequencyUnit unit) : this(value * unit.HertzAmount)
        {
        }

        [Pure] public TimeInterval ToResolution() => TimeInterval.Second / Hertz;

        [Pure] public double ToHz() => this / Hz;
        [Pure] public double ToKHz() => this / KHz;
        [Pure] public double ToMHz() => this / MHz;
        [Pure] public double ToGHz() => this / GHz;

        public static readonly Frequency Hz = FrequencyUnit.Hz.ToFrequency();
        public static readonly Frequency KHz = FrequencyUnit.KHz.ToFrequency();
        public static readonly Frequency MHz = FrequencyUnit.MHz.ToFrequency();
        public static readonly Frequency GHz = FrequencyUnit.GHz.ToFrequency();

        public static Frequency FromHz(double value) => Hz * value;
        public static Frequency FromKHz(double value) => KHz * value;
        public static Frequency FromMHz(double value) => MHz * value;
        public static Frequency FromGHz(double value) => GHz * value;

        public static implicit operator Frequency(double value) => new Frequency(value);
        public static implicit operator double(Frequency property) => property.Hertz;

        public static double operator /(Frequency a, Frequency b) => 1.0 * a.Hertz / b.Hertz;
        public static Frequency operator /(Frequency a, double k) => new Frequency(a.Hertz / k);
        public static Frequency operator *(Frequency a, double k) => new Frequency(a.Hertz * k);
        public static Frequency operator *(double k, Frequency a) => new Frequency(a.Hertz * k);

        public override string ToString() => Hertz + " " + FrequencyUnit.Hz.Name;
    }
}