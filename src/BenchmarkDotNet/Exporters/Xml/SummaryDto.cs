using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

// ReSharper disable UnusedMember.Global

namespace BenchmarkDotNet.Exporters.Xml
{
    internal class SummaryDto
    {
        public string Title => summary.Title;

        public HostEnvironmentInfoDto HostEnvironmentInfo =>
            new HostEnvironmentInfoDto(summary.HostEnvironmentInfo);

        [PublicAPI]
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
        public string ProcessorName => ProcessorBrandStringHelper.Prettify(hei.CpuInfo.Value);
        public string PhysicalProcessorCount => hei.CpuInfo.Value?.PhysicalProcessorCount?.ToString();
        public string PhysicalCoreCount => hei.CpuInfo.Value?.PhysicalCoreCount?.ToString();
        public string LogicalCoreCount => hei.CpuInfo.Value?.LogicalCoreCount?.ToString();
        public string RuntimeVersion => hei.RuntimeVersion;
        public string Architecture => hei.Architecture;
        public bool HasAttachedDebugger => hei.HasAttachedDebugger;
        public bool HasRyuJit => hei.HasRyuJit;
        public string Configuration => hei.Configuration;
        public string DotNetSdkVersion => hei.DotNetSdkVersion.Value;
        public ChronometerDto ChronometerFrequency => new ChronometerDto(hei.ChronometerFrequency);
        public string HardwareTimerKind => hei.HardwareTimerKind.ToString();

        private readonly HostEnvironmentInfo hei;

        public HostEnvironmentInfoDto(HostEnvironmentInfo hei) => this.hei = hei;
    }

    internal class ChronometerDto
    {
        public double Hertz => frequency.Hertz;

        private readonly Frequency frequency;

        public ChronometerDto(Frequency frequency) => this.frequency = frequency;
    }

    internal class BenchmarkReportDto
    {
        public string DisplayInfo => report.BenchmarkCase.DisplayInfo;
        public string Namespace => report.BenchmarkCase.Descriptor.Type.Namespace;
        public string Type => report.BenchmarkCase.Descriptor.Type.Name;
        public string Method => report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
        public string MethodTitle => report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo;
        public string Parameters => report.BenchmarkCase.Parameters.PrintInfo;
        public Statistics Statistics => report.ResultStatistics;
        public IEnumerable<Metric> Metrics => report.Metrics.Values;
        public GcStats Memory => new GcStats()
        {
            Gen0Collections = report.GcStats.Gen0Collections,
            Gen1Collections = report.GcStats.Gen1Collections,
            Gen2Collections = report.GcStats.Gen2Collections,
            TotalOperations = report.GcStats.TotalOperations,
            BytesAllocatedPerOperation = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase)
        };
        [PublicAPI] public IEnumerable<Measurement> Measurements { get; }

        private readonly BenchmarkReport report;

        public BenchmarkReportDto(BenchmarkReport report, bool excludeMeasurements = false)
        {
            this.report = report;
            Measurements = excludeMeasurements ? null : report.AllMeasurements;
        }
    }

    /// <summary>
    /// This type is used to ensure that the allocated bytes are persisted in the XML
    /// report when serialized, as the original <see cref="Engines.GcStats"/> type does
    /// not contain a property for the value so the report would otherwise lack it.
    /// See https://github.com/dotnet/BenchmarkDotNet/pull/1919 for more details.
    /// </summary>
    internal struct GcStats
    {
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long TotalOperations { get; set; }
        public long? BytesAllocatedPerOperation { get; set; }
    }
}
