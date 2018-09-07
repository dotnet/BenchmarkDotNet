using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Reports
{
    public class Metric : IEquatable<Metric>
    {
        private const string NoUnit = "";

        public string UniqueName { get; }

        public double Value { get; }
        
        public string Legend { get; }
        
        public string NumberFormat { get; }

        public UnitType UnitType { get; }
        
        public string Unit { get; }

        public bool TheGreaterTheBetter { get; }
        
        public Metric(string uniqueName, double value, string legend, string numberFormat = "0.##", UnitType unitType = UnitType.Dimensionless, string unit = NoUnit, bool theGreaterTheBetter = false)
        {
            UniqueName = uniqueName;
            Value = value;
            Legend = legend;
            NumberFormat = numberFormat;
            Unit = unit;
            UnitType = unitType;
            TheGreaterTheBetter = theGreaterTheBetter;
        }

        public bool Equals(Metric other) => string.Equals(UniqueName, other.UniqueName);

        public override bool Equals(object obj) => obj is Metric metric && Equals(metric);

        public override int GetHashCode() => UniqueName.GetHashCode();
    }
}