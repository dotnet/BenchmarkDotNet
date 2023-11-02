using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.Reports;
using JetBrains.Annotations;
using VerifyXunit;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    [Collection("VerifyTests")]
    [UsesVerify]
    public class CommonExporterVerifyTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public CommonExporterVerifyTests()
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
        [MemberData(nameof(CultureInfoNames))]
        public Task Exporters(string cultureInfoName)
        {
            var cultureInfo = CultureInfos[cultureInfoName];
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            var logger = new AccumulationLogger();

            var exporters = GetExporters();
            foreach (var exporter in exporters)
            {
                PrintTitle(logger, exporter);
                exporter.ExportToLog(
                    MockFactory.CreateSummary(
                        config.WithCultureInfo(cultureInfo),
                        hugeSd: false,
                        new[]
                        {
                            new Metric(new FakeMetricDescriptor("CacheMisses", "Hardware counter 'CacheMisses' per single operation", "N0"), 7)
                        }), logger);
            }

            var settings = VerifySettingsFactory.Create();
            settings.UseTextForParameters(GetName(cultureInfo));
            return Verifier.Verify(logger.GetLog(), settings);
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
            .AddHardwareCounters(HardwareCounter.CacheMisses)
            .AddColumnProvider(DefaultColumnProviders.Metrics)
            .AddDiagnoser(Diagnosers.MemoryDiagnoser.Default);

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = initCulture;
        }
    }
}