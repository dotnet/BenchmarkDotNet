using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class RPlotExporter : IExporter, IExporterDependancies
    {
        public static readonly IExporter Default = new RPlotExporter();

        private static object buildScriptLock = new object();

        public IEnumerable<IExporter> Dependencies
        {
            // R Plots depends on having the full measurments available
            get { yield return CsvMeasurementsExporter.Default; }
        }

        public IEnumerable<string> ExportToFiles(Summary summary)
        {
            const string scriptFileName = "BuildPlots.R";
            yield return scriptFileName;

            var fileNamePrefix = Path.Combine(summary.ResultsDirectoryPath, summary.Title);
            var scriptFullPath = Path.Combine(summary.ResultsDirectoryPath, scriptFileName);
            var script = ResourceHelper.
                LoadTemplate(scriptFileName).
                Replace("$BenchmarkDotNetVersion$", BenchmarkDotNetInfo.FullTitle).
                Replace("$CsvSeparator$", CsvMeasurementsExporter.Default.Separator);
            lock (buildScriptLock)
                File.WriteAllText(scriptFullPath, script);

            // TODO: implement smart autodetection of the R bin folder
            var rHome = Environment.GetEnvironmentVariable("R_HOME");
            if (Directory.Exists(rHome))
            {
                var start = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                    FileName = Path.Combine(rHome, "Rscript.exe"),
                    WorkingDirectory = summary.ResultsDirectoryPath,
                    Arguments = $"\"{scriptFullPath}\" \"{fileNamePrefix}-measurements.csv\""
                };
                using (var process = Process.Start(start))
                    process?.WaitForExit();
                yield return fileNamePrefix + "-boxplot.png";
                yield return fileNamePrefix + "-barplot.png";
            }
            else
            {
                // TODO: print warning, if the folder is not found
            }
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            throw new NotSupportedException();
        }
    }
}