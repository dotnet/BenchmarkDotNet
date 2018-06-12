﻿using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
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

            CheckResults(5, measurements, OutlierMode.OnlyUpper);
            CheckResults(5, measurements, OutlierMode.All);
            
            CheckResults(6, measurements, OutlierMode.None);
            CheckResults(6, measurements, OutlierMode.OnlyLower);
        }

        [AssertionMethod]
        private static void CheckResults(int expectedResultCount, List<Measurement> measurements, OutlierMode outlierMode)
        {
            Assert.Equal(expectedResultCount, new RunResults(null, measurements, outlierMode, default, default).GetMeasurements().Count());
        }

        private static void Add(List<Measurement> measurements, int time)
        {
            measurements.Add(new Measurement(1, IterationMode.MainTarget, measurements.Count + 1, 1, time));
        }
    }
}