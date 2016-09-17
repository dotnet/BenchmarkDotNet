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

        public TimeInterval ToResolution() => TimeInterval.Second / Hertz;
        public override string ToString() => Hertz + " " + FrequencyUnit.Hz.Name;

        public static readonly Frequency Hz = FrequencyUnit.Hz.ToFrequency();
        public static readonly Frequency KHz = FrequencyUnit.KHz.ToFrequency();
        public static readonly Frequency MHz = FrequencyUnit.MHz.ToFrequency();
        public static readonly Frequency GHz = FrequencyUnit.GHz.ToFrequency();

        public static implicit operator Frequency(double value) => new Frequency(value);
        public static implicit operator double(Frequency property) => property.Hertz;

        public static double operator /(Frequency a, Frequency b) => 1.0 * a.Hertz / b.Hertz;
    }
}