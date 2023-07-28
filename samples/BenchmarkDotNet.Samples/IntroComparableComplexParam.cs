using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroComparableComplexParam
    {
        [ParamsSource(nameof(ValuesForA))]
        public ComplexParam? A { get; set; }

        public IEnumerable<ComplexParam> ValuesForA => new[] { new ComplexParam(1, "First"), new ComplexParam(2, "Second") };

        [Benchmark]
        public object? Benchmark() => A;

        // Only non generic IComparable is required to provide custom order behavior, but implementing IComparable<> too is customary.
        public class ComplexParam : IComparable<ComplexParam>, IComparable
        {
            public ComplexParam(int value, string name)
            {
                Value = value;
                Name = name;
            }

            public int Value { get; set; }

            public string Name { get; set; }

            public override string ToString() => Name;

            public int CompareTo(ComplexParam? other) => other == null ? 1 : Value.CompareTo(other.Value);

            public int CompareTo(object obj) => obj is ComplexParam other ? CompareTo(other) : throw new ArgumentException();
        }
    }
}