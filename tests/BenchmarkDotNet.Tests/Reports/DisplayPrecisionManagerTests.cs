using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Reports
{
    public class DisplayPrecisionManagerTests
    {
        private readonly ITestOutputHelper output;

        public DisplayPrecisionManagerTests(ITestOutputHelper output) => this.output = output;

        private class TestData
        {
            public double[] Values { get; }

            public List<(int? ParentPrecision, int ExpectedPrecision)> Configurations { get; }

            public TestData(double[] values, int ppNull, int pp1, int pp2, int pp3, int pp4)
            {
                Values = values;
                Configurations = new List<(int? ParentPrecision, int ExpectedPrecision)>
                {
                    (null, ppNull),
                    (1, pp1),
                    (2, pp2),
                    (3, pp3),
                    (4, pp4)
                };
            }
        }

        private static readonly Dictionary<string, TestData> TestDataItems = new Dictionary<string, TestData>
        {
            { "Min=1000     ", new TestData(new[] { 1000.0 }, 1, 1, 2, 3, 4) },
            { "Min= 100     ", new TestData(new[] { 0100.0 }, 1, 1, 2, 3, 4) },
            { "Min=  10     ", new TestData(new[] { 010.00 }, 2, 2, 2, 3, 4) },
            { "Min=   1     ", new TestData(new[] { 01.000 }, 3, 2, 3, 3, 4) },
            { "Min=   0.1   ", new TestData(new[] { 0.1000 }, 4, 2, 3, 4, 4) },
            { "Min=   0.01  ", new TestData(new[] { 0.0100 }, 4, 2, 3, 4, 4) },
            { "Min=   0.001 ", new TestData(new[] { 0.0010 }, 4, 2, 3, 4, 4) },
            { "Min=   0.0001", new TestData(new[] { 0.0001 }, 4, 2, 3, 4, 4) },
            { "Min=   0.0000", new TestData(new[] { 0.0000 }, 1, 1, 2, 3, 4) },
            { "Empty", new TestData(Array.Empty<double>(), 1, 1, 2, 3, 4) },
            { "NaN", new TestData(new[] { double.NaN }, 1, 1, 2, 3, 4) },
            { "Infinity", new TestData(new[] { double.PositiveInfinity }, 1, 1, 2, 3, 4) }
        };

        [UsedImplicitly]
        public static TheoryData<string> TestDataNames => TheoryDataHelper.Create(TestDataItems.Keys);

        [Theory]
        [MemberData(nameof(TestDataNames))]
        public void GeneralTest(string testDataName)
        {
            var testData = TestDataItems[testDataName];

            output.WriteLine("Values: [" + string.Join(";", testData.Values.Select(v => v.ToString("0.##", TestCultureInfo.Instance))) + "]");

            foreach (var configuration in testData.Configurations)
            {
                (var parentPrecision, int expectedPrecision) = configuration;

                int actualPrecision = parentPrecision.HasValue
                    ? DisplayPrecisionManager.CalcPrecision(testData.Values, parentPrecision.Value)
                    : DisplayPrecisionManager.CalcPrecision(testData.Values);

                string strParent = parentPrecision.HasValue ? 1234.5678.ToString("N" + parentPrecision, TestCultureInfo.Instance) : "NA";
                var strValues = testData.Values.Select(v => v.ToString("N" + actualPrecision, TestCultureInfo.Instance)).ToList();
                int maxWidth = strValues.Any() ? Math.Max(strValues.Max(s => s.Length), strParent.Length) + 6 : 0;
                int parentWidth = maxWidth - (actualPrecision - parentPrecision) ?? 0;

                output.WriteLine("******************************");
                output.WriteLine("Parent   Precision: " + (parentPrecision.HasValue ? parentPrecision.Value.ToString() : "<NA>"));
                output.WriteLine("Actual   Precision: " + actualPrecision);
                output.WriteLine("Expected Precision: " + expectedPrecision);
                output.WriteLine("Parent Value:");
                output.WriteLine(strParent.PadLeft(parentWidth));
                output.WriteLine("Actual Formatted Values:");
                foreach (string strValue in strValues)
                    output.WriteLine(strValue.PadLeft(maxWidth));
                output.WriteLine(expectedPrecision == actualPrecision ? "CORRECT" : "ERROR");

                Assert.Equal(expectedPrecision, actualPrecision);
            }
        }

        [Theory]
        [InlineData(4, 0.01)]
        [InlineData(4, 0.123456)]
        [InlineData(4, 0.1)]
        [InlineData(1, 0.0)]
        [InlineData(4, 0.9)]
        [InlineData(3, 1)]
        [InlineData(3, 1.5)]
        [InlineData(3, 9.999999999)]
        [InlineData(2, 10)]
        [InlineData(2, 99.99999999)]
        [InlineData(1, 100)]
        [InlineData(1, 999.9999999)]
        [InlineData(1, 10000)]
        [InlineData(1, 100000)]
        [InlineData(1, -100000)]
        [InlineData(1, double.NaN)]
        [InlineData(1, double.PositiveInfinity)]
        public void ClassicTest(int expected, double value)
        {
            int actual = DisplayPrecisionManager.CalcPrecision(new[] { value });
            Assert.Equal(expected, actual);
        }
    }
}