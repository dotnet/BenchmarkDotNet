using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JsonSerializer = SimpleJson.SimpleJson;

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
            // We construct HostEnvironmentInfo manually, so that we can have the HardwareTimerKind enum as text, rather than an integer
            // SimpleJson serializer doesn't seem to have an enum String/Value option (to-be-fair, it is meant to be "Simple")
            var environmentInfo = new
            {
                HostEnvironmentInfo.BenchmarkDotNetCaption,
                summary.HostEnvironmentInfo.BenchmarkDotNetVersion,
                OsVersion = summary.HostEnvironmentInfo.OsVersion.Value,
                ProcessorName = ProcessorBrandStringHelper.Prettify(summary.HostEnvironmentInfo.CpuInfo.Value),
                summary.HostEnvironmentInfo.CpuInfo.Value?.PhysicalProcessorCount,
                summary.HostEnvironmentInfo.CpuInfo.Value?.PhysicalCoreCount,
                summary.HostEnvironmentInfo.CpuInfo.Value?.LogicalCoreCount,
                summary.HostEnvironmentInfo.RuntimeVersion,
                summary.HostEnvironmentInfo.Architecture,
                summary.HostEnvironmentInfo.HasAttachedDebugger,
                summary.HostEnvironmentInfo.HasRyuJit,
                summary.HostEnvironmentInfo.Configuration,
                summary.HostEnvironmentInfo.JitModules,
                DotNetCliVersion = summary.HostEnvironmentInfo.DotNetSdkVersion.Value,
                summary.HostEnvironmentInfo.ChronometerFrequency,
                HardwareTimerKind = summary.HostEnvironmentInfo.HardwareTimerKind.ToString()
            };

            // If we just ask SimpleJson to serialise the entire "summary" object it throws several errors.
            // So we are more specific in what we serialise (plus some fields/properties aren't relevant)

            var benchmarks = summary.Reports.Select(report =>
            {
                var data = new Dictionary<string, object>
                {
                    // We don't need Benchmark.ShortInfo, that info is available via Benchmark.Parameters below
                    { "DisplayInfo", report.BenchmarkCase.DisplayInfo },
                    { "Namespace", report.BenchmarkCase.Descriptor.Type.Namespace },
                    { "Type", FullNameProvider.GetTypeName(report.BenchmarkCase.Descriptor.Type) },
                    { "Method", report.BenchmarkCase.Descriptor.WorkloadMethod.Name },
                    { "MethodTitle", report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo },
                    { "Parameters", report.BenchmarkCase.Parameters.PrintInfo },
                    { "FullName", FullNameProvider.GetBenchmarkName(report.BenchmarkCase) }, // do NOT remove this property, it is used for xunit-performance migration
                    // { "Properties", r.Benchmark.Job.ToSet().ToDictionary(p => p.Name, p => p.Value) }, // TODO
                    { "Statistics", report.ResultStatistics }
                };

                // We show MemoryDiagnoser's results only if it is being used
                if(report.BenchmarkCase.Config.HasMemoryDiagnoser())
                {
                    data.Add("Memory", report.GcStats);
                }
                
                if (ExcludeMeasurements == false)
                {
                    // We construct Measurements manually, so that we can have the IterationMode enum as text, rather than an integer
                    data.Add("Measurements",
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
                        data.Add("Metrics", report.Metrics.Values);
                    }
                }

                return data;
            });

            JsonSerializer.CurrentJsonSerializerStrategy.Indent = IndentJson;
            logger.WriteLine(JsonSerializer.SerializeObject(new Dictionary<string, object>
            {
                { "Title", summary.Title },
                { "HostEnvironmentInfo", environmentInfo },
                { "Benchmarks", benchmarks }
            }));
        }
    }
}