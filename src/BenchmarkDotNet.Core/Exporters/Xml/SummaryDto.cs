using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Xml
{
    internal class SummaryDto
    {
        public string Title => summary.Title;

        public HostEnvironmentInfoDto HostEnvironmentInfo =>
            new HostEnvironmentInfoDto(summary.HostEnvironmentInfo);

        public IEnumerable<BenchmarkReportDto> Benchmarks { get; }

        private readonly Summary summary;

        public SummaryDto(Summary summary, bool excludeMeasurements = false)
        {
            this.summary = summary;
            Benchmarks = summary.Reports.Select(
                report => new BenchmarkReportDto(report, excludeMeasurements));
        }
    }

    internal class HostEnvironmentInfoDto
    {
        public string BenchmarkDotNetCaption => HostEnvironmentInfo.BenchmarkDotNetCaption;
        public string BenchmarkDotNetVersion => hei.BenchmarkDotNetVersion;
        public string OsVersion => hei.OsVersion.Value;
        public string ProcessorName => hei.ProcessorName.Value;
        public int ProcessorCount => hei.ProcessorCount;
        public string RuntimeVersion => hei.RuntimeVersion;
        public string Architecture => hei.Architecture;
        public bool HasAttachedDebugger => hei.HasAttachedDebugger;
        public bool HasRyuJit => hei.HasRyuJit;
        public string Configuration => hei.Configuration;
        public string JitModules => hei.JitModules;
        public string DotNetSdkVersion => hei.DotNetSdkVersion.Value;
        public ChronometerDto ChronometerFrequency => new ChronometerDto(hei.ChronometerFrequency);
        public string HardwareTimerKind => hei.HardwareTimerKind.ToString();

        private readonly HostEnvironmentInfo hei;

        public HostEnvironmentInfoDto(HostEnvironmentInfo hei)
        {
            this.hei = hei;
        }
    }

    internal class ChronometerDto
    {
        public double Hertz => frequency.Hertz;

        private Frequency frequency;

        public ChronometerDto(Frequency frequency)
        {
            this.frequency = frequency;
        }
    }

    internal class BenchmarkReportDto
    {
        public string DisplayInfo => report.Benchmark.DisplayInfo;
        public string Namespace => report.Benchmark.Target.Type.Namespace;
        public string Type => report.Benchmark.Target.Type.Name;
        public string Method => report.Benchmark.Target.Method.Name;
        public string MethodTitle => report.Benchmark.Target.MethodDisplayInfo;
        public string Parameters => report.Benchmark.Parameters.PrintInfo;
        public Statistics Statistics => report.ResultStatistics;
        public GcStats Memory => report.GcStats;
        public IEnumerable<Measurement> Measurements { get; }

        private readonly BenchmarkReport report;

        public BenchmarkReportDto(BenchmarkReport report, bool excludeMeasurements = false)
        {
            this.report = report;

            if (excludeMeasurements)
                Measurements = null;
            else
                Measurements = report.AllMeasurements;
        }
    }
}
