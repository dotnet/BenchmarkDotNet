using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
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

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
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

            var rscriptExecutable = RuntimeInformation.IsWindows() ? "Rscript.exe" : "Rscript";
            string rscriptPath;
            var rHome = Environment.GetEnvironmentVariable("R_HOME");
            if (rHome != null)
            {
                rscriptPath = Path.Combine(rHome, "bin", rscriptExecutable);
                if (!File.Exists(rscriptPath))
                {
                    consoleLogger.WriteLineError($"RPlotExporter requires R_HOME to point to the directory containing bin{Path.DirectorySeparatorChar}{rscriptExecutable} (currently points to {rHome})");
                    yield break;
                }
            }
            else // No R_HOME, try the path
            {
                rscriptPath = FindInPath(rscriptExecutable);
                if (rscriptPath == null)
                {
                    consoleLogger.WriteLineError($"RPlotExporter couldn't find {rscriptExecutable} in your PATH and no R_HOME environment variable is defined");
                    yield break;
                }
            }

            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = false,
                CreateNoWindow = true,
                FileName = rscriptPath,
                WorkingDirectory = summary.ResultsDirectoryPath,
                Arguments = $"\"{scriptFullPath}\" \"{fileNamePrefix}-measurements.csv\""
            };
            using (var process = Process.Start(start))
                process?.WaitForExit();
            yield return fileNamePrefix + "-boxplot.png";
            yield return fileNamePrefix + "-barplot.png";
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            throw new NotSupportedException();
        }

        static string FindInPath(string name) => Environment.GetEnvironmentVariable("PATH")
            .Split(Path.PathSeparator)
            .Select(p => Path.Combine(p, name))
            .FirstOrDefault(File.Exists);
    }
}