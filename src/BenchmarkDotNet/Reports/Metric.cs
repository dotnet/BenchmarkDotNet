namespace BenchmarkDotNet.Reports
{
    public struct Metric
    {
        public const string NoUnit = "";

        public string Name { get; }

        public double Value { get; }

        public string Unit { get; }

        public bool TheGreaterTheBetter { get; }
        
        public Metric(string name, double value, string unit = NoUnit, bool theGreaterTheBetter = false)
        {
            Name = name;
            Value = value;
            Unit = unit;
            TheGreaterTheBetter = theGreaterTheBetter;
        }
    }
}