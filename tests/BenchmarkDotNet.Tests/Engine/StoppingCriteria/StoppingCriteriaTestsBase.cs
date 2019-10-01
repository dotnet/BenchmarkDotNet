using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine.StoppingCriteria
{
    [Collection("StoppingCriteriaTests")]
    public abstract class StoppingCriteriaTestsBase
    {
        protected readonly ITestOutputHelper Output;

        protected StoppingCriteriaTestsBase(ITestOutputHelper output) => Output = output;

        [AssertionMethod]
        protected void ResolutionsAreCorrect(IStoppingCriteria criteria, double[] values, int expectedCount)
        {
            var measurements = Generate(values);
            for (int iteration = 1; iteration <= measurements.Count; iteration++)
            {
                var subMeasurements = measurements.Take(iteration).ToList();
                var resolution = criteria.Evaluate(subMeasurements);
                bool actualIsFinished = resolution.IsFinished;
                bool expectedIsFinished = iteration >= expectedCount;
                Output.WriteLine($"#{iteration} " +
                                 $"Expected: {expectedIsFinished}, " +
                                 $"Actual: {actualIsFinished}{(resolution.Message != null ? $" [{resolution.Message}]" : "")}, " +
                                 $"Measurements: <{string.Join(",", values.Take(iteration))}>");
                Assert.Equal(expectedIsFinished, actualIsFinished);
            }
        }

        private static IReadOnlyList<Measurement> Generate(params double[] values)
        {
            var measurements = new List<Measurement>(values.Length);
            for (int i = 1; i <= values.Length; i++)
                measurements.Add(new Measurement(1, IterationMode.Unknown, IterationStage.Unknown, i, 1, values[i - 1]));
            return measurements;
        }

        public class Warnings
        {
            public static readonly Warnings Empty = new Warnings();

            public string[] Messages { get; }

            public Warnings(params string[] messages) => Messages = messages;

            public override string ToString() => $"Messages: {Messages.Length}";
        }

        public class WaringTestData
        {
            private IStoppingCriteria Criteria { get; }
            private Warnings ExpectedWarnings { get; }

            public WaringTestData(IStoppingCriteria criteria, Warnings expectedWarnings)
            {
                Criteria = criteria;
                ExpectedWarnings = expectedWarnings;
            }

            public void Deconstruct(out IStoppingCriteria criteria, out Warnings expectedWarnings)
            {
                criteria = Criteria;
                expectedWarnings = ExpectedWarnings;
            }
        }
    }
}