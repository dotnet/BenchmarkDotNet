using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    // In case of failed approval tests, use the following reporter:
    // [UseReporter(typeof(KDiffReporter))]
    [Collection("ApprovalTests")]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    public class CommonExporterApprovalTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public CommonExporterApprovalTests()
        {
            initCulture = Thread.CurrentThread.CurrentCulture;
        }

        private static readonly Dictionary<string, CultureInfo> CultureInfos = new Dictionary<string, CultureInfo>
        {
            { "", CultureInfo.InvariantCulture },
            { "ru-RU", new CultureInfo("ru-RU") },
            { "en-US", new CultureInfo("en-US") }
        };

        [UsedImplicitly]
        public static TheoryData<string> CultureInfoNames => TheoryDataHelper.Create(CultureInfos.Keys);

        [Theory]
        [MethodImpl(MethodImplOptions.NoInlining)] // required by the Approval test framework, do NOT remove
        [MemberData(nameof(CultureInfoNames))]
        public void Exporters(string cultureInfoName)
        {
            var cultureInfo = CultureInfos[cultureInfoName];
            NamerFactory.AdditionalInformation = $"{GetName(cultureInfo)}";
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            var logger = new AccumulationLogger();

            var exporters = GetExporters();
            foreach (var exporter in exporters)
            {
                PrintTitle(logger, exporter);
                exporter.ExportToLog(MockFactory.CreateSummary(config.WithCultureInfo(cultureInfo)), logger);
            }

            Approvals.Verify(logger.GetLog(), x => x.Replace("\r", string.Empty));
        }

        private static void PrintTitle(AccumulationLogger logger, IExporter exporter)
        {
            logger.WriteLine("############################################");
            logger.WriteLine($"{exporter.Name}");
            logger.WriteLine("############################################");
        }

        private static string GetName(CultureInfo cultureInfo)
        {
            if (cultureInfo.Name == string.Empty)
                return "Invariant";
            return cultureInfo.Name;
        }

        private static IEnumerable<IExporter> GetExporters()
        {
            //todo add CsvExporter and CsvMeasurementsExporter (need to mock RuntimeInformation)
            yield return AsciiDocExporter.Default;
            yield return HtmlExporter.Default;
            yield return JsonExporter.Brief;
            yield return JsonExporter.BriefCompressed;
            yield return JsonExporter.Full;
            yield return JsonExporter.FullCompressed;
            yield return MarkdownExporter.Default;
            yield return MarkdownExporter.Atlassian;
            yield return MarkdownExporter.Console;
            yield return MarkdownExporter.GitHub;
            yield return MarkdownExporter.StackOverflow;
            yield return PlainExporter.Default;
            yield return XmlExporter.Brief;
            yield return XmlExporter.BriefCompressed;
            yield return XmlExporter.Full;
            yield return XmlExporter.FullCompressed;
        }

        private static readonly IConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.StdDev)
            .AddColumn(StatisticColumn.P67)
            .AddDiagnoser(Diagnosers.MemoryDiagnoser.Default);

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = initCulture;
        }
    }
}