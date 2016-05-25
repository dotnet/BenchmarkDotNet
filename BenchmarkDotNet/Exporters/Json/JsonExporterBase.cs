using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System.Linq;
using JsonSerialiser = SimpleJson.SimpleJson;

namespace BenchmarkDotNet.Exporters.Json
{
    public abstract class JsonExporterBase : ExporterBase
    {
        protected override string FileExtension => "json";

        private bool IndentJson { get; set; }
        private bool ExcludeMeasurments { get; set; }

        public JsonExporterBase(bool indentJson = false, bool excludeMeasurments = false)
        {
            IndentJson = indentJson;
            ExcludeMeasurments = excludeMeasurments;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            // We construct HostEnvironmentInfo manually, so that we can have the HardwareTimerKind enum as text, rather than an integer
            // SimpleJson serialiser doesn't seem to have an enum String/Value option (to-be-fair, it is meant to be "Simple")
            var environmentInfo = new
            {
                summary.HostEnvironmentInfo.BenchmarkDotNetCaption,
                summary.HostEnvironmentInfo.BenchmarkDotNetVersion,
                summary.HostEnvironmentInfo.OsVersion,
                summary.HostEnvironmentInfo.ProcessorName,
                summary.HostEnvironmentInfo.ProcessorCount,
                summary.HostEnvironmentInfo.ClrVersion,
                summary.HostEnvironmentInfo.Architecture,
                summary.HostEnvironmentInfo.HasAttachedDebugger,
                summary.HostEnvironmentInfo.HasRyuJit,
                summary.HostEnvironmentInfo.Configuration,
                summary.HostEnvironmentInfo.JitModules,
                DotNetCliVersion = summary.HostEnvironmentInfo.DotNetCliVersion.Value,
                summary.HostEnvironmentInfo.ChronometerFrequency,
                HardwareTimerKind = summary.HostEnvironmentInfo.HardwareTimerKind.ToString()
            };

            // If we just ask SimpleJson to serialise the entire "summary" object it throws several errors.
            // So we are more specific in what we serialise (plus some fields/properties aren't relevant)
            logger.Write(JsonSerialiser.SerializeObject(new
            {
                summary.Title,
                HostEnvironmentInfo = environmentInfo,
                Benchmarks = summary.Reports.Select(r => new
                {
                    // We don't need Benchmark.ShortInfo, that info is available via Benchmark.Parameters below
                    r.Benchmark.ShortInfo,
                    Type = r.Benchmark.Target.Type.Name,
                    Method = r.Benchmark.Target.Method.Name,
                    r.Benchmark.Target.MethodTitle,
                    Parameters = r.Benchmark.Parameters.PrintInfo,
                    Properties = r.Benchmark.Job.AllProperties.ToDictionary(p => p.Name, p => p.Value),
                    Statistics = r.ResultStatistics,
                    // We construct Measurment manually, so that we can have the IterationMode enum as text, rather than an integer
                    Measurements = 
                        (ExcludeMeasurments ? Enumerable.Empty<Measurement>() : r.AllMeasurements)
                        .Select(m => new
                        {
                            IterationMode = m.IterationMode.ToString(),
                            m.LaunchIndex,
                            m.IterationIndex,
                            m.Operations,
                            m.Nanoseconds
                        })
                })
            }));
        }
    }
}
