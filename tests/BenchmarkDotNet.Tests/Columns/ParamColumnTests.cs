using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns
{
    public class ParamColumnTests
    {
        [Theory]
        [InlineData("Field", "Field", "str", "str")]
        [InlineData("Field", "Field", true, "True")]
        [InlineData("Field", "Field", null, ParameterInstance.NullParameterTextRepresentation)]
        [InlineData("Field", "F", "str", ParameterInstance.UnknownParameterTextRepresentation)]
        public void GetValueTest(string columnName, string parameterName, object parameterValue, string expected)
        {
            var instance = CreateParameterInstance(parameterName, parameterValue);
            var summary = CreateMockSummary(instance);

            var column = new ParamColumn(columnName);
            var actual = column.GetValue(summary, summary.BenchmarksCases.First(), summary.Style);
            Assert.Equal(expected, actual);
        }

        private static ParameterInstance CreateParameterInstance(string name, object value)
        {
            var summaryStyle = new SummaryStyle(TestCultureInfo.Instance, false, null, null);

            var parameterType = value?.GetType() ?? typeof(object);
            var definition = new ParameterDefinition(name, false, new[] { value }, false, parameterType, 0);
            return new ParameterInstance(definition, definition.Values.First(), summaryStyle);
        }

        private static Summary CreateMockSummary(ParameterInstance instance)
        {
            var benchmarkCase = new BenchmarkCase(
                new Descriptor(null, null),
                Job.Dry,
                new ParameterInstances(new ParameterInstance[] { instance }),
                ImmutableConfigBuilder.Create(new ManualConfig()));

            var benchmarkReport = new BenchmarkReport(true, benchmarkCase, null, null, null, null);
            return new Summary("", new[] { benchmarkReport }.ToImmutableArray(), HostEnvironmentInfo.GetCurrent(),
                "", "", TimeSpan.Zero, CultureInfo.InvariantCulture, ImmutableArray<ValidationError>.Empty, ImmutableArray<IColumnHidingRule>.Empty);
        }
    }
}
