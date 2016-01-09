using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Common;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public static class BenchmarkExporterHelper
    {
        public static string ExportToFile(IBenchmarkExporter exporter, IList<BenchmarkReport> reports, string fileNamePrefix)
        {
            var fileName = fileNamePrefix + "-report." + exporter.Name;
            using (var stream = new StreamWriter(fileName))
                exporter.Export(reports, new BenchmarkStreamLogger(stream));
            return fileName;
        }

        // TODO: signature refactoring
        public static List<string[]> BuildTable(IList<BenchmarkReport> reports, bool pretty = true)
        {
            var data = reports.
                Where(r => r.GetTargetRuns().Any()).
                Select(r => new
                {
                    r.Benchmark,
                    Report = r,
                    Stat = r.GetTargetRuns().GetStats()
                }).ToList();
            if (data.Count == 0)
                return new List<string[]>();

            var stats = data.Select(item => item.Stat).ToList();
            var timeUnit = TimeUnit.GetBestTimeUnit(stats.Select(t => t.Mean).ToArray());

            var showParams = false;
            var headerRow = new List<string> { "Type", "Method", "Mode", "Platform", "Jit", ".NET", "toolchain", "Runtime", "Warmup", "Target" };
            if (data.Any(r => !r.Benchmark.Task.ParametersSets.IsEmpty()))
            {
                // TODO: write generic logic for multiple parameters
                headerRow.Add("IntParam");
                showParams = true;
            }
            headerRow.Add("AvrTime");
            headerRow.Add("Error");

            var orderedData = data;
            // For https://github.com/PerfDotNet/BenchmarkDotNet/issues/36
            if (showParams)
                orderedData = data.
                    OrderBy(r => r.Report.Parameters.IntParam).
                    ThenBy(r => r.Benchmark.Target.Type.Name).
                    ToList();

            var table = new List<string[]> { headerRow.ToArray() };
            foreach (var item in orderedData)
            {
                var b = item.Benchmark;

                var row = new List<string>
                {
                    b.Target.Type.Name,
                    b.Target.MethodTitle,
                    b.Task.Configuration.Mode.ToString(),
                    b.Task.Configuration.Platform.ToString(),
                    b.Task.Configuration.JitVersion.ToString(),
                    b.Task.Configuration.Framework.ToString(),
                    b.Task.Configuration.Toolchain.ToString(),
                    b.Task.Configuration.Runtime.ToString(),
                    b.Task.Configuration.WarmupIterationCount.ToString(),
                    b.Task.Configuration.TargetIterationCount.ToString()
                };

                if (showParams)
                    row.Add(item.Report.Parameters.IntParam.ToString());
                row.Add(item.Stat.Mean.ToTimeStr(timeUnit));
                row.Add(item.Stat.StandardError.ToTimeStr(timeUnit));

                table.Add(row.ToArray());
            }

            return table;
        }
    }
}