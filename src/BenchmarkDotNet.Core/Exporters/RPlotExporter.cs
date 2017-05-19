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
    public class RPlotExporter : IExporter, IExporterDependencies
    {
        public static readonly IExporter Default = new RPlotExporter();
        public string Name => nameof(RPlotExporter);

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

            string fileNamePrefix = Path.Combine(summary.ResultsDirectoryPath, summary.Title);
            string scriptFullPath = Path.Combine(summary.ResultsDirectoryPath, scriptFileName);
            string script = ResourceHelper.
                LoadTemplate(scriptFileName).
                Replace("$BenchmarkDotNetVersion$", BenchmarkDotNetInfo.FullTitle).
                Replace("$CsvSeparator$", CsvMeasurementsExporter.Default.Separator);
            lock (buildScriptLock)
                File.WriteAllText(scriptFullPath, script);

            string rscriptExecutable = RuntimeInformation.IsWindows() ? "Rscript.exe" : "Rscript";
            string rscriptPath;
            string rHome = Environment.GetEnvironmentVariable("R_HOME");
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

        public static string FindInPath(string fileName)
        {
            var dirs = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);
            foreach (string dir in dirs)
            {
                string trimmedDir = dir.Trim('\'', '"');
                try
                {
                    string filePath = Path.Combine(trimmedDir, fileName);
                    if (File.Exists(filePath))
                        return filePath;
                }
                catch (Exception)
                {
                    // Nevermind
                }
            }
            return null;
        }
    }
}