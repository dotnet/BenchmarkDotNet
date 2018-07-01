namespace BenchmarkDotNet.Horology
{
    public class FrequencyUnit
    {
        public string Name { get; }
        public string Description { get; }
        public long HertzAmount { get; }

        private FrequencyUnit(string name, string description, long hertzAmount)
        {
            Name = name;
            Description = description;
            HertzAmount = hertzAmount;
        }
        
        public static readonly FrequencyUnit Hz = new FrequencyUnit("Hz", "Hertz", 1);
        public static readonly FrequencyUnit KHz = new FrequencyUnit("KHz", "Kilohertz", 1000);
        public static readonly FrequencyUnit MHz = new FrequencyUnit("MHz", "Megahertz", 1000 * 1000);
        public static readonly FrequencyUnit GHz = new FrequencyUnit("GHz", "Gigahertz", 1000 * 1000 * 1000);
        public static readonly FrequencyUnit[] All = { Hz, KHz, MHz, GHz };

        public Frequency ToFrequency(long value = 1) => new Frequency(value, this);
    }
}