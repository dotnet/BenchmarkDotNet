using System;
using BenchmarkDotNet.Engines;
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
            FullBenchmarkName = GetBenchmarkName(benchmarkCase);
            JobId = benchmarkCase.Job.Id;
        }

        public int Value { get; }

        private string JobId { get; }

        private string FullBenchmarkName { get; }

        [PublicAPI] public bool Equals(BenchmarkId other) => Value == other.Value;

        public override bool Equals(object obj) => throw new InvalidOperationException("boxing");

        public override int GetHashCode() => Value;

        public string ToArguments() => $"--benchmarkName {FullBenchmarkName.EscapeCommandLine()} --job {JobId.EscapeCommandLine()} --benchmarkId {Value}";

        public string ToArguments(string fromBenchmark, string toBenchmark) => $"{AnonymousPipesHost.AnonymousPipesDescriptors} {fromBenchmark} {toBenchmark} {ToArguments()}";

        public override string ToString() => Value.ToString();

        private static string GetBenchmarkName(BenchmarkCase benchmark)
        {
            var fullName = FullNameProvider.GetBenchmarkName(benchmark);

            // FullBenchmarkName is passed to Process.Start as an argument and each OS limits the max argument length, so we have to limit it too
            // Windows limit is 32767 chars, Unix is 128*1024 but we use 1024 as a common sense limit
            if (fullName.Length < 1024)
                return fullName;

            string typeName = FullNameProvider.GetTypeName(benchmark.Descriptor.Type);
            string methodName = benchmark.Descriptor.WorkloadMethod.Name;
            string paramsHash = benchmark.HasParameters
                ? "paramsHash_" + Hashing.HashString(FullNameProvider.GetMethodName(benchmark)).ToString()
                : string.Empty;

            return $"{typeName}.{methodName}({paramsHash})";
        }
    }
}