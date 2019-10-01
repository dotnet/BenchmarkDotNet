using System;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    /// <summary>
    /// represents an internal entity used to identify a benchmark within an executable with multiple benchmarks
    /// </summary>
    public struct BenchmarkId
    {
        public BenchmarkId(int value, BenchmarkCase benchmarkCase)
        {
            Value = value;
            FullBenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase);
            JobId = benchmarkCase.Job.Id;
        }

        public int Value { get; }

        private string JobId { get; }

        private string FullBenchmarkName { get; }

        [PublicAPI] public bool Equals(BenchmarkId other) => Value == other.Value;

        public override bool Equals(object obj) => throw new InvalidOperationException("boxing");

        public override int GetHashCode() => Value;

        public string ToArguments() => $"--benchmarkName {FullBenchmarkName.Escape()} --job {JobId.Escape()} --benchmarkId {Value}";

        public override string ToString() => Value.ToString();
    }
}