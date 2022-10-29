using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests.Mocks.Toolchain
{
    public class MockMeasurer : IMockMeasurer
    {
        private readonly Func<BenchmarkCase, List<Measurement>> measure;

        private MockMeasurer(Func<BenchmarkCase, List<Measurement>> measure) => this.measure = measure;

        public List<Measurement> Measure(BenchmarkCase benchmarkCase) => measure(benchmarkCase);

        private static List<Measurement> CreateFromValues(double[] values) => values
            .Select((value, i) => new Measurement(1, IterationMode.Workload, IterationStage.Result, i, 1, value))
            .ToList();

        public static IMockMeasurer Create(Func<BenchmarkCase, List<Measurement>> measure) => new MockMeasurer(measure);

        public static IMockMeasurer Create(Func<string, double[]> measure) =>
            new MockMeasurer(benchmarkCase => CreateFromValues(measure(benchmarkCase.Descriptor.WorkloadMethod.Name)));
    }
}