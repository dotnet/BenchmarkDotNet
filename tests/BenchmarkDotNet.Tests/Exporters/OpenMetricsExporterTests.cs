using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.OpenMetrics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Infra;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.Reports;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using VerifyXunit;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    [Collection("VerifyTests")]
    public class OpenMetricsExporterTests
    {
        [Fact]
        public Task SingleBenchmark_ProducesHelpAndTypeOnce()
        {
            var summary = new Summary(
                "SingleBenchmarkSummary",
                [
                    new BenchmarkReport(
                        success: true,
                        benchmarkCase: new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances([
                                new ParameterInstance(new ParameterDefinition("param1", false, ["Parameter 1"], true, typeof(string), 0 ), "value1", SummaryStyle.Default),
                            ]),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label", "label"), 42.0)
                        ]),
                    new BenchmarkReport(
                        success: true,
                        benchmarkCase: new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances([
                                new ParameterInstance(new ParameterDefinition("param1", false, ["Parameter 1"], true, typeof(string), 0 ), "value2", SummaryStyle.Default),
                            ]),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label", "label"), 42.0)
                        ]),
                    new BenchmarkReport(
                        success: true,
                        benchmarkCase: new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances([
                                new ParameterInstance(new ParameterDefinition("param1", false, ["Parameter 1"], true, typeof(string), 0 ), "value3", SummaryStyle.Default),
                            ]),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label", "label"), 42.0)
                        ])
                ],
                HostEnvironmentInfo.GetCurrent(),
                "",
                "",
                TimeSpan.Zero,
                CultureInfo.InvariantCulture,
                ImmutableArray<ValidationError>.Empty,
                ImmutableArray<IColumnHidingRule>.Empty);

            var logger = new AccumulationLogger();

            OpenMetricsExporter.Default.ExportToLog(summary, logger);

            var settings = VerifyHelper.Create();
            return Verifier.Verify(logger.GetLog(), settings);
        }

        [Fact]
        public Task ParametrizedBenchmarks_LabelExpansion()
        {
var summary = new Summary(
                "SingleBenchmarkSummary",
                [
                    new BenchmarkReport(
                        success: true,
                        benchmarkCase: new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances([
                                new ParameterInstance(new ParameterDefinition("param1", false, ["Parameter 1"], true, typeof(string), 0 ), "value1", SummaryStyle.Default),
                                new ParameterInstance(new ParameterDefinition("param2", false, ["Parameter 2"], true, typeof(string), 0 ), "value1", SummaryStyle.Default),
                                new ParameterInstance(new ParameterDefinition("param3", false, ["Parameter 3"], true, typeof(string), 0 ), "value1", SummaryStyle.Default)
                            ]),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label", "label"), 42.0)
                        ]),
                    new BenchmarkReport(
                        success: true,
                        benchmarkCase: new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances([
                                new ParameterInstance(new ParameterDefinition("param1", false, ["Parameter 1"], true, typeof(string), 0 ), "value2", SummaryStyle.Default),
                                new ParameterInstance(new ParameterDefinition("param2", false, ["Parameter 2"], true, typeof(string), 0 ), "value2", SummaryStyle.Default),
                                new ParameterInstance(new ParameterDefinition("param3", false, ["Parameter 3"], true, typeof(string), 0 ), "value2", SummaryStyle.Default)                            ]),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label", "label"), 42.0)
                        ]),
                    new BenchmarkReport(
                        success: true,
                        benchmarkCase: new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances([
                                new ParameterInstance(new ParameterDefinition("param1", false, ["Parameter 1"], true, typeof(string), 0 ), "value3", SummaryStyle.Default),
                                new ParameterInstance(new ParameterDefinition("param2", false, ["Parameter 2"], true, typeof(string), 0 ), "value3", SummaryStyle.Default),
                                new ParameterInstance(new ParameterDefinition("param3", false, ["Parameter 3"], true, typeof(string), 0 ), "value3", SummaryStyle.Default)                            ]),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label", "label"), 42.0)
                        ])
                ],
                HostEnvironmentInfo.GetCurrent(),
                "",
                "",
                TimeSpan.Zero,
                CultureInfo.InvariantCulture,
                ImmutableArray<ValidationError>.Empty,
                ImmutableArray<IColumnHidingRule>.Empty);
            var logger = new AccumulationLogger();

            OpenMetricsExporter.Default.ExportToLog(summary, logger);

            var settings = VerifyHelper.Create();
            return Verifier.Verify(logger.GetLog(), settings);
        }

        [Fact]
        public Task LabelsAreEscapedCorrectly()
        {
            var summary = new Summary(
                "",
                [
                    new BenchmarkReport(
                        true,
                        new BenchmarkCase(
                            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                            Job.Dry,
                            new ParameterInstances(ImmutableArray<ParameterInstance>.Empty),
                            ImmutableConfigBuilder.Create(new ManualConfig())),
                        null!,
                        null!,
                        [
                            new ExecuteResult([
                                new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 10, 1)
                            ])
                        ],
                        [
                            new(new FakeMetricDescriptor("label_with_underscore", "label with underscore"), 42.0),
                            new(new FakeMetricDescriptor("label_with-dash", "label with dash"), 84.0),
                            new(new FakeMetricDescriptor("label with space", "label with space"), 126.0),
                            new(new FakeMetricDescriptor("label.with.dot", "label with dot"), 168.0),
                            new(new FakeMetricDescriptor("label with special chars !@#$%^&*()", "label with special chars !@#$%^&*()"), 210.0),
                            new(new FakeMetricDescriptor("label with special !@#$%^&*() chars", "label with special !@#$%^&*() chars"), 210.0),
                            new(new FakeMetricDescriptor("label with special !@#$%^&*()chars in the middle", "label with special !@#$%^&*()chars in the middle"), 210.0)
                        ])
                ],
                HostEnvironmentInfo.GetCurrent(),
                "",
                "",
                TimeSpan.Zero,
                CultureInfo.InvariantCulture,
                ImmutableArray<ValidationError>.Empty,
                ImmutableArray<IColumnHidingRule>.Empty);
            var logger = new AccumulationLogger();

            OpenMetricsExporter.Default.ExportToLog(summary, logger);

            var settings = VerifyHelper.Create();
            return Verifier.Verify(logger.GetLog(), settings);
        }

        [Fact]
        public Task DecimalSeparator_UsesInvariantCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // Uses comma as decimal separator

                var summary = new Summary(
                    "DecimalSeparatorTest",
                    [
                        new BenchmarkReport(
                            success: true,
                            benchmarkCase: new BenchmarkCase(
                                new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                                Job.Dry,
                                new ParameterInstances(ImmutableArray<ParameterInstance>.Empty),
                                ImmutableConfigBuilder.Create(new ManualConfig())),
                            null!,
                            null!,
                            [
                                new ExecuteResult([
                                    new Measurement(0, IterationMode.Workload, IterationStage.Result, 1, 1, 10.5)
                                ])
                            ],
                            [])
                    ],
                    HostEnvironmentInfo.GetCurrent(),
                    "",
                    "",
                    TimeSpan.Zero,
                    CultureInfo.InvariantCulture,
                    ImmutableArray<ValidationError>.Empty,
                    ImmutableArray<IColumnHidingRule>.Empty);

                var logger = new AccumulationLogger();

                OpenMetricsExporter.Default.ExportToLog(summary, logger);

                var log = logger.GetLog();
                // Verify that the value uses a period, not a comma
                Assert.Contains("10.5", log);
                Assert.DoesNotContain("10,5", log);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }

            return Task.CompletedTask;
        }
    }
}