using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using JsonSerialiser = SimpleJson.SimpleJson;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Exporters.Json
{
    public abstract class JsonExporterBase : ExporterBase
    {
        protected override string FileExtension => "json";

        private bool IndentJson { get; set; }
        private bool ExcludeMeasurements { get; set; }

        public JsonExporterBase(bool indentJson = false, bool excludeMeasurements = false)
        {
            IndentJson = indentJson;
            ExcludeMeasurements = excludeMeasurements;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            // We construct HostEnvironmentInfo manually, so that we can have the HardwareTimerKind enum as text, rather than an integer
            // SimpleJson serialiser doesn't seem to have an enum String/Value option (to-be-fair, it is meant to be "Simple")
            var environmentInfo = new
            {
                HostEnvironmentInfo.BenchmarkDotNetCaption,
                summary.HostEnvironmentInfo.BenchmarkDotNetVersion,
                OsVersion = summary.HostEnvironmentInfo.OsVersion.Value,
                ProcessorName = summary.HostEnvironmentInfo.ProcessorName.Value,
                summary.HostEnvironmentInfo.ProcessorCount,
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

            var benchmarks = summary.Reports.Select(r =>
            {
                var data = new Dictionary<string, object>
                {
                    // We don't need Benchmark.ShortInfo, that info is available via Benchmark.Parameters below
                    { "DisplayInfo", r.Benchmark.DisplayInfo },
                    { "Namespace", r.Benchmark.Target.Type.Namespace },
                    { "Type", r.Benchmark.Target.Type.Name },
                    { "Method", r.Benchmark.Target.Method.Name },
                    { "MethodTitle", r.Benchmark.Target.MethodDisplayInfo },
                    { "Parameters", r.Benchmark.Parameters.PrintInfo },
                    // { "Properties", r.Benchmark.Job.ToSet().ToDictionary(p => p.Name, p => p.Value) }, // TODO
                    { "Statistics", r.ResultStatistics },
                };

                // We show MemoryDiagnoser's results only if it is being used
                if(summary.Config.GetDiagnosers().Any(diagnoser => diagnoser is MemoryDiagnoser))
                {
                    data.Add("Memory", r.GcStats);
                }
                
                if (ExcludeMeasurements == false)
                {
                    // We construct Measurements manually, so that we can have the IterationMode enum as text, rather than an integer
                    data.Add("Measurements",
                        r.AllMeasurements.Select(m => new
                        {
                            IterationMode = m.IterationMode.ToString(),
                            m.LaunchIndex,
                            m.IterationIndex,
                            m.Operations,
                            m.Nanoseconds
                        }));
                }

                return data;
            });

            JsonSerialiser.CurrentJsonSerializerStrategy.Indent = IndentJson;
            logger.WriteLine(JsonSerialiser.SerializeObject(new Dictionary<string, object>
            {
                { "Title", summary.Title },
                { "HostEnvironmentInfo", environmentInfo },
                { "Benchmarks", benchmarks }
            }));
        }
    }
}