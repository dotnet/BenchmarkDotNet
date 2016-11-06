using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
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

            CheckResults(5, measurements, true);
            CheckResults(6, measurements, false);
        }

        private static void CheckResults(int expectedResultCount, List<Measurement> measurements, bool removeOutliers)
        {
            Assert.Equal(expectedResultCount, new RunResults(null, measurements, removeOutliers, default(GcStats)).GetMeasurements().Count());
        }

        private static void Add(List<Measurement> measurements, int time)
        {
            measurements.Add(new Measurement(1, IterationMode.MainTarget, measurements.Count + 1, 1, time));
        }
    }
}