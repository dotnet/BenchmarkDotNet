using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private static readonly object buildScriptLock = new object();

        public IEnumerable<IExporter> Dependencies
        {
            // R Plots depends on having the full measurements available
            get { yield return CsvMeasurementsExporter.Default; }
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            const string scriptFileName = "BuildPlots.R";
            const string logFileName = "BuildPlots.log";
            yield return scriptFileName;

            string fileNamePrefix = Path.Combine(summary.ResultsDirectoryPath, summary.Title);
            string csvFullPath = CsvMeasurementsExporter.Default.GetArtifactFullName(summary);
            
            string scriptFullPath = Path.Combine(summary.ResultsDirectoryPath, scriptFileName);
            string logFullPath = Path.Combine(summary.ResultsDirectoryPath, logFileName);
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
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                FileName = rscriptPath,
                WorkingDirectory = summary.ResultsDirectoryPath,
                Arguments = $"\"{scriptFullPath}\" \"{csvFullPath}\""
            };
            using (var process = Process.Start(start))
            {
                string output = process?.StandardOutput.ReadToEnd() ?? "";
                string error = process?.StandardError.ReadToEnd() ?? "";
                File.WriteAllText(logFullPath, output + Environment.NewLine + error);
                process?.WaitForExit();
            }

            yield return fileNamePrefix + "-boxplot.png";
            yield return fileNamePrefix + "-barplot.png";
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            throw new NotSupportedException();
        }

        private static string FindInPath(string fileName)
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
                    // Never mind
                }
            }
            return null;
        }
    }
}