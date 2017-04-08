#if CLASSIC
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
namespace BenchmarkDotNet.Tests.Exporters
{
    [UseReporter(typeof(XUnit2Reporter))]
    public class ApprovalTest : IDisposable
    {
        private readonly CultureInfo initCulture;

        public ApprovalTest()
        {
            initCulture = Thread.CurrentThread.CurrentCulture;
        }

        [Theory]
        [MemberData(nameof(GetExporters))]
        public void Exporter(IExporter exporter, CultureInfo cultureInfo)
        {
            NamerFactory.AdditionalInformation = $"{exporter.Name}_{cultureInfo.DisplayName}";
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            var logger = new AccumulationLogger();
            exporter.ExportToLog(MockFactory.CreateSummary(config), logger);

            Approvals.Verify(logger.GetLog());
        }

        private static TheoryData<IExporter, CultureInfo> GetExporters()
        {
            var exporters = new List<IExporter>()
            {
                AsciiDocExporter.Default,
//              new CsvExporter(CsvSeparator.CurrentCulture), //not ready until RuntimeInformation will be mocked
//              new CsvMeasurementsExporter(CsvSeparator.CurrentCulture), //need to be checked
                HtmlExporter.Default,
                JsonExporter.Brief,
                JsonExporter.BriefCompressed,
                JsonExporter.Full,
                JsonExporter.FullCompressed,
                MarkdownExporter.Default,
                MarkdownExporter.Atlassian,
                MarkdownExporter.Console,
                MarkdownExporter.GitHub,
                MarkdownExporter.StackOverflow,
                PlainExporter.Default
            };
            var cultures = new List<CultureInfo>()
            {
                CultureInfo.InvariantCulture,
                new CultureInfo("ru-RU"),
                new CultureInfo("en-US")
            };

            var theoryData = new TheoryData<IExporter, CultureInfo>();
            foreach (var exporter in exporters)
            foreach (var cultureInfo in cultures)
                theoryData.Add(exporter, cultureInfo);
            return theoryData;
        }

        private static readonly IConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .With(StatisticColumn.Mean)
            .With(StatisticColumn.StdDev)
            .With(StatisticColumn.P67);

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = initCulture;
        }
    }
}
#endif