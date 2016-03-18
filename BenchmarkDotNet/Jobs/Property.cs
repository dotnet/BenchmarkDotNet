using System;

namespace BenchmarkDotNet.Jobs
{
    public struct Property : IEquatable<Property>
    {
        public string Name { get; }

        public string Value { get; }

        public Property(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string GetShortInfo()
        {
            switch (Name)
            {
                case "Mode":
                case "Platform":
                    return Value;
                case "Warmup":
                case "Target":
                case "Process":
                case "IterationTime":
                case "Affinity":
                    return Name + Value;
            }

            return $"{Name}-{Value}";
        }

        public bool Equals(Property other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is Property && Equals((Property)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Property left, Property right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Property left, Property right)
        {
            return !left.Equals(right);
        }
    }
}