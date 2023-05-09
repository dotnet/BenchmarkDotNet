using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroComparableComplexParam
    {
        // property with public setter
        [ParamsSource(nameof(ValuesForA))]
        public ComplexParam A { get; set; }

        // public property
        public IEnumerable<ComplexParam> ValuesForA => new[] { new ComplexParam(1, "First"), new ComplexParam(2, "Second") };

        [Benchmark]
        public object Benchmark() => A;

        // Only non generic IComparable is required, but implementing IComparable<> too is customary.
        public class ComplexParam : IComparable<ComplexParam>, IComparable
        {
            public ComplexParam(int value, string name)
            {
                Value = value;
                Name = name;
            }

            public int Value { get; set; }

            public string Name { get; set; }

            public override string ToString()
            {
                return Name;
            }

            public int CompareTo(ComplexParam other)
            {
                if (other == null)
                {
                    return 1;
                }

                return Value.CompareTo(other.Value);
            }

            public int CompareTo(object obj)
            {
                if (obj == null)
                {
                    return 1;
                }

                if (obj is not ComplexParam other)
                {
                    throw new ArgumentException();
                }

                return CompareTo(other);
            }
        }
    }
}