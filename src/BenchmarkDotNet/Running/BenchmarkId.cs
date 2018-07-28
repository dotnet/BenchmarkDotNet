using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    /// <summary>
    /// represents an internal entity used to identify a benchmark within an executable with multiple benchmarks
    /// </summary>
    public struct BenchmarkId
    {
        public BenchmarkId(int value) => Value = value;

        public int Value { get; }

        [PublicAPI] public bool Equals(BenchmarkId other) => Value == other.Value;

        public override bool Equals(object obj) => throw new InvalidOperationException("boxing");

        public override int GetHashCode() => Value;

        public string ToArgument() => $"-benchmarkId {Value}";

        public override string ToString() => Value.ToString();
    }
}