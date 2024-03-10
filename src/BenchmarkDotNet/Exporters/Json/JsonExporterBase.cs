using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Perfolizer.Helpers;
using Perfolizer.Horology;
using SimpleJson;

namespace BenchmarkDotNet.Exporters.Json
{
    public abstract class JsonExporterBase : ExporterBase
    {
        protected override string FileExtension => "json";

        private bool IndentJson { get; }
        private bool ExcludeMeasurements { get; }

        protected JsonExporterBase(bool indentJson = false, bool excludeMeasurements = false)
        {
            IndentJson = indentJson;
            ExcludeMeasurements = excludeMeasurements;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            SimpleJsonSerializer.CurrentJsonSerializerStrategy.Indent = IndentJson;
            logger.WriteLine(SimpleJsonSerializer.SerializeObject(GetDataToSerialize(summary)));
        }

        protected virtual IReadOnlyDictionary<string, object> GetDataToSerialize(Summary summary)
        {
            // If we just ask SimpleJson to serialize the entire "summary" object it throws several errors.
            // So we are more specific in what we serialize (plus some fields/properties aren't relevant)
            return new Dictionary<string, object>
            {
                { "Title", summary.Title },
                { "HostEnvironmentInfo", GetDataToSerialize(summary.HostEnvironmentInfo) },
                { "Benchmarks", summary.Reports.Select(GetDataToSerialize) }
            };
        }

        protected virtual IReadOnlyDictionary<string, object> GetDataToSerialize(HostEnvironmentInfo environmentInfo)
        {
            // We construct HostEnvironmentInfo manually, so that we can have the HardwareTimerKind enum as text, rather than an integer
            // SimpleJson serializer doesn't seem to have an enum String/Value option (to-be-fair, it is meant to be "Simple")
            return new Dictionary<string, object>
            {
                { nameof(HostEnvironmentInfo.BenchmarkDotNetCaption), HostEnvironmentInfo.BenchmarkDotNetCaption },
                { nameof(environmentInfo.BenchmarkDotNetVersion), environmentInfo.BenchmarkDotNetVersion },
                { "OsVersion", environmentInfo.Os.Value.ToBrandString() },
                { "ProcessorName", environmentInfo.Cpu.Value.ToShortBrandName() },
                { "PhysicalProcessorCount", environmentInfo.Cpu.Value?.PhysicalProcessorCount },
                { "PhysicalCoreCount", environmentInfo.Cpu.Value?.PhysicalCoreCount },
                { "LogicalCoreCount", environmentInfo.Cpu.Value?.LogicalCoreCount },
                { nameof(environmentInfo.RuntimeVersion), environmentInfo.RuntimeVersion },
                { nameof(environmentInfo.Architecture), environmentInfo.Architecture },
                { nameof(environmentInfo.HasAttachedDebugger), environmentInfo.HasAttachedDebugger },
                { nameof(environmentInfo.HasRyuJit), environmentInfo.HasRyuJit },
                { nameof(environmentInfo.Configuration), environmentInfo.Configuration },
                { "DotNetCliVersion", environmentInfo.DotNetSdkVersion.Value },
                { nameof(environmentInfo.ChronometerFrequency), environmentInfo.ChronometerFrequency },
                { nameof(HardwareTimerKind), environmentInfo.HardwareTimerKind.ToString() },
            };
        }

        protected virtual IReadOnlyDictionary<string, object> GetDataToSerialize(BenchmarkReport report)
        {
            var benchmark = new Dictionary<string, object>
            {
                // We don't need Benchmark.ShortInfo, that info is available via Benchmark.Parameters below
                { "DisplayInfo", report.BenchmarkCase.DisplayInfo },
                { "Namespace", report.BenchmarkCase.Descriptor.Type.Namespace },
                { "Type", FullNameProvider.GetTypeName(report.BenchmarkCase.Descriptor.Type) },
                { "Method", report.BenchmarkCase.Descriptor.WorkloadMethod.Name },
                { "MethodTitle", report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo },
                { "Parameters", report.BenchmarkCase.Parameters.PrintInfo },
                {
                    "FullName", FullNameProvider.GetBenchmarkName(report.BenchmarkCase)
                }, // do NOT remove this property, it is used for xunit-performance migration
                // Hardware Intrinsics can be disabled using env vars, that is why they might be different per benchmark and are not exported as part of HostEnvironmentInfo
                { "HardwareIntrinsics", report.GetHardwareIntrinsicsInfo() ?? "" },
                // { "Properties", r.Benchmark.Job.ToSet().ToDictionary(p => p.Name, p => p.Value) }, // TODO
                { "Statistics", report.ResultStatistics }
            };

            // We show MemoryDiagnoser's results only if it is being used
            if (report.BenchmarkCase.Config.HasMemoryDiagnoser())
            {
                benchmark.Add("Memory", new
                {
                    report.GcStats.Gen0Collections,
                    report.GcStats.Gen1Collections,
                    report.GcStats.Gen2Collections,
                    report.GcStats.TotalOperations,
                    BytesAllocatedPerOperation = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase)
                });
            }

            if (ExcludeMeasurements == false)
            {
                // We construct Measurements manually, so that we can have the IterationMode enum as text, rather than an integer
                benchmark.Add("Measurements",
                    report.AllMeasurements.Select(m => new
                    {
                        IterationMode = m.IterationMode.ToString(),
                        IterationStage = m.IterationStage.ToString(),
                        m.LaunchIndex,
                        m.IterationIndex,
                        m.Operations,
                        m.Nanoseconds
                    }));

                if (report.Metrics.Any())
                {
                    benchmark.Add("Metrics", report.Metrics.Values);
                }
            }

            return benchmark;
        }
    }
}