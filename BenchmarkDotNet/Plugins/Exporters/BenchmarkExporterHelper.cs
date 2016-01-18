using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Plugins.ResultExtenders;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public static class BenchmarkExporterHelper
    {
        // TODO: signature refactoring
        public static List<string[]> BuildTable(IList<BenchmarkReport> reports,
                                                IEnumerable<IBenchmarkResultExtender> resultExtenders = null,
                                                bool pretty = true)
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

            var orderedData = data;
            // For https://github.com/PerfDotNet/BenchmarkDotNet/issues/36
            if (showParams)
                orderedData = data.
                    OrderBy(r => r.Report.Parameters.IntParam).
                    ThenBy(r => r.Benchmark.Target.Type.Name).
                    ToList();

            IList<IList<string>> extraColumns = null;
            if (resultExtenders != null)
            {
                extraColumns = new List<IList<string>>();
                var resultsToProcess = orderedData.Select(s => Tuple.Create(s.Report, s.Stat)).ToList();
                foreach (var extender in resultExtenders)
                {
                    var column = extender.GetExtendedResults(resultsToProcess, timeUnit);
                    // This behaviour/restriction is outlined in IBenchmarkResultExtender.cs
                    if (column != null && column.Count == resultsToProcess.Count)
                    {
                        headerRow.Add(extender.ColumnName);
                        extraColumns.Add(column);
                    }
                    // TODO log an error if the two column counts  don't match (not sure where/how though?!)
                }
            }

            var table = new List<string[]> { headerRow.ToArray() };
            var rowNumber = 0;
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

                if (extraColumns != null)
                {
                    foreach (var column in extraColumns)
                        row.Add(column[rowNumber]);
                }

                table.Add(row.ToArray());
                rowNumber++;
            }

            return table;
        }
    }
}