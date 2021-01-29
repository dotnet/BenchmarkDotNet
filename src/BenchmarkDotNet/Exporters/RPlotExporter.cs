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

        private const string ImageExtension = ".png";
        private static readonly object BuildScriptLock = new object();

        public IEnumerable<IExporter> Dependencies
        {
            // R Plots depends on having the full measurements available
            get { yield return CsvMeasurementsExporter.Default; }
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            const string scriptFileName = "BuildPlots.R";
            const string logFileName = "BuildPlots.log";
            yield return Path.Combine(summary.ResultsDirectoryPath, scriptFileName);

            string csvFullPath = CsvMeasurementsExporter.Default.GetArtifactFullName(summary);
            string scriptFullPath = Path.Combine(summary.ResultsDirectoryPath, scriptFileName);
            string logFullPath = Path.Combine(summary.ResultsDirectoryPath, logFileName);
            string script = ResourceHelper.
                LoadTemplate(scriptFileName).
                Replace("$BenchmarkDotNetVersion$", BenchmarkDotNetInfo.FullTitle).
                Replace("$CsvSeparator$", CsvMeasurementsExporter.Default.Separator);
            lock (BuildScriptLock)
                File.WriteAllText(scriptFullPath, script);

            if (!TryFindRScript(consoleLogger, out string rscriptPath))
            {
                yield break;
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
            using (var process = new Process {StartInfo = start})
            using (AsyncProcessOutputReader reader = new AsyncProcessOutputReader(process))
            {
                // When large R scripts are generated then ran, ReadToEnd()
                // causes the stdout and stderr buffers to become full,
                // which causes R to hang. To avoid this, use
                // AsyncProcessOutputReader to cache the log contents
                // then write to disk rather than Process.Standard*.ReadToEnd().
                process.Start();
                reader.BeginRead();
                process.WaitForExit();
                reader.StopRead();
                File.WriteAllLines(logFullPath, reader.GetOutputLines());
                File.AppendAllLines(logFullPath, reader.GetErrorLines());
            }

            yield return Path.Combine(summary.ResultsDirectoryPath, $"*{ImageExtension}");
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            throw new NotSupportedException();
        }

        private static bool TryFindRScript(ILogger consoleLogger, out string rscriptPath)
        {
            string rscriptExecutable = RuntimeInformation.IsWindows() ? "Rscript.exe" : "Rscript";
            rscriptPath = null;
            string rHome = Environment.GetEnvironmentVariable("R_HOME");
            if (rHome != null)
            {
                rscriptPath = Path.Combine(rHome, "bin", rscriptExecutable);
                if (File.Exists(rscriptPath))
                {
                    return true;
                }

                consoleLogger.WriteLineError($"RPlotExporter requires R_HOME to point to the parent directory of the existing '{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}{rscriptExecutable} (currently points to {rHome})");
            }

            // No R_HOME, or R_HOME points to a wrong folder, try the path
            rscriptPath = FindInPath(rscriptExecutable);
            if (rscriptPath == null)
            {
                consoleLogger.WriteLineError($"RPlotExporter couldn't find {rscriptExecutable} in your PATH and no R_HOME environment variable is defined");
                return false;
            }

            return true;
        }

        private static string FindInPath(string fileName)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (path == null)
                return null;

            var dirs = path.Split(Path.PathSeparator);
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
