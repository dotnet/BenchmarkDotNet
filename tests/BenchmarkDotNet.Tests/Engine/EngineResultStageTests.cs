using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.OutlierDetection;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineResultStageTests
    {
        [Fact]
        public void OutliersTest()
        {
            var measurements = new List<Measurement>();
            Add(measurements, 100);
            Add(measurements, 101);
            Add(measurements, 102);
            Add(measurements, 103);
            Add(measurements, 104);
            Add(measurements, 500); // It's an outlier

            CheckResults(5, measurements, OutlierMode.RemoveUpper);
            CheckResults(5, measurements, OutlierMode.RemoveAll);

            CheckResults(6, measurements, OutlierMode.DontRemove);
            CheckResults(6, measurements, OutlierMode.RemoveLower);
        }

        [AssertionMethod]
        private static void CheckResults(int expectedResultCount, List<Measurement> measurements, OutlierMode outlierMode)
        {
            Assert.Equal(expectedResultCount, new RunResults(measurements, outlierMode, default, default, 0).GetWorkloadResultMeasurements().Count());
        }

        private static void Add(List<Measurement> measurements, int time)
        {
            measurements.Add(new Measurement(1, IterationMode.Workload, IterationStage.Actual, measurements.Count + 1, 1, time));
        }
    }
}