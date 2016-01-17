using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkRPlotExporter : IBenchmarkExporter
    {
        public string Name => "RPlot";
        public string Description => "RPlot exporter";

        public static readonly IBenchmarkExporter Default = new BenchmarkRPlotExporter();

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            throw new System.NotSupportedException();
        }

        public IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string fileNamePrefix, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            const string scriptFileName = "BuildPlots.R";
            yield return scriptFileName;

            var dir = new FileInfo(fileNamePrefix + ".fake").Directory?.FullName ?? "./";
            var scriptFullPath = Path.Combine(dir, scriptFileName);
            File.WriteAllText(scriptFullPath, ResourceHelper.LoadTemplate(scriptFileName));

            var rHome = Environment.GetEnvironmentVariable("R_HOME") ?? @"C:\Program Files\R\R-3.2.3\bin\";
            if (Directory.Exists(rHome))
            {
                var start = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                    FileName = Path.Combine(rHome, "Rscript.exe"),
                    Arguments = $"{scriptFullPath} {fileNamePrefix}-runs.csv"
                };
                using (var process = Process.Start(start))
                {
                }
                yield return fileNamePrefix + "-boxplot.png";
                yield return fileNamePrefix + "-barplot.png";
            }
        }
    }
}