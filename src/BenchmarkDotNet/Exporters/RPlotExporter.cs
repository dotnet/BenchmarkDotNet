using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Detectors;
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
                Replace("$BenchmarkDotNetVersion$", BenchmarkDotNetInfo.Instance.BrandTitle).
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
            using (var process = new Process { StartInfo = start })
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

        private static bool TryFindRScript(ILogger consoleLogger, out string? rscriptPath)
        {
            string rscriptExecutable = OsDetector.IsWindows() ? "Rscript.exe" : "Rscript";
            rscriptPath = null;

            string rHome = Environment.GetEnvironmentVariable("R_HOME");
            if (rHome != null)
            {
                rscriptPath = Path.Combine(rHome, "bin", rscriptExecutable);
                if (File.Exists(rscriptPath))
                    return true;

                consoleLogger.WriteLineError($"{nameof(RPlotExporter)} requires R_HOME to point to the parent directory of the existing '{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}{rscriptExecutable} (currently points to {rHome})");
                return false;
            }

            // No R_HOME, or R_HOME points to a wrong folder, try the path
            rscriptPath = FindInPath(rscriptExecutable);
            if (rscriptPath != null)
                return true;

            if (OsDetector.IsWindows())
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesR = Path.Combine(programFiles, "R");
                if (Directory.Exists(programFilesR))
                {
                    foreach (string rRootDirectory in Directory.EnumerateDirectories(programFilesR))
                    {
                        string rscriptPathCandidate = Path.Combine(rRootDirectory, "bin", rscriptExecutable);
                        if (File.Exists(rscriptPathCandidate))
                        {
                            rscriptPath = rscriptPathCandidate;
                            return true;
                        }
                    }
                }
            }

            consoleLogger.WriteLineError($"{nameof(RPlotExporter)} couldn't find {rscriptExecutable} in your PATH and no R_HOME environment variable is defined");
            return false;
        }

        private static string? FindInPath(string fileName)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (path == null)
                return null;

            string[] dirs = path.Split(Path.PathSeparator);
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

        public List<string> GetExpectedPngPaths(Summary summary, ILogger consoleLogger)
        {
            const string scriptFileName = "get-png-paths-script.R";
            var pngPaths = new List<string>();

            // Create R script content
            string script = @"
if (!require(dplyr)) install.packages('dplyr')
library(dplyr)

ends_with <- function(vars, match, ignore.case = TRUE) {
  if (ignore.case) match <- tolower(match)
  n <- nchar(match)
  if (ignore.case) vars <- tolower(vars)
  length <- nchar(vars)
  substr(vars, pmax(1, length - n + 1), length) == match
}

list_benchmark_png_paths <- function(measurement_files) {
  paths <- c()
  for (file in measurement_files) {
    title <- gsub('-measurements.csv', '', basename(file))
    paths <- c(paths,
              gsub('-measurements.csv', '-boxplot.png', file),
              gsub('-measurements.csv', '-barplot.png', file))
    
    measurements <- read.csv(file, sep = ',')
    result <- measurements %>% filter(Measurement_IterationStage == 'Result')
    
    for (target in unique(result$Target_Method)) {
      paths <- c(paths,
                gsub('-measurements.csv', paste0('-', target, '-density.png'), file),
                gsub('-measurements.csv', paste0('-', target, '-facetDensity.png'), file),
                gsub('-measurements.csv', paste0('-', target, '-facetTimeline.png'), file),
                gsub('-measurements.csv', paste0('-', target, '-facetTimelineSmooth.png'), file),
                gsub('-measurements.csv', paste0('-', target, '-cummean.png'), file))
      
      df <- result %>% filter(Target_Method == target)
      
      for (params in unique(df$Params)) {
        if (!is.na(params)) {
          prefix <- paste0('-', target, '-', params)
          paths <- c(paths,
                    gsub('-measurements.csv', paste0(prefix, '-density.png'), file),
                    gsub('-measurements.csv', paste0(prefix, '-facetDensity.png'), file))
        }
      }
      
      for (job in unique(df$Job_Id)) {
        if (!is.na(job)) {
          paths <- c(paths,
                    gsub('-measurements.csv', paste0('-', target, '-', job, '-timeline.png'), file),
                    gsub('-measurements.csv', paste0('-', target, '-', job, '-timelineSmooth.png'), file),
                    gsub('-measurements.csv', paste0('-', target, '-', job, '-cummean.png'), file),
                    gsub('-measurements.csv', paste0('-', target, '-', job, '-density.png'), file))
        }
      }
    }
  }
  return(unique(paths))
}

files <- list.files(pattern = '*-measurements.csv')
if (length(files) > 0) {
  png_paths <- list_benchmark_png_paths(files)
  cat(png_paths, sep='\n')
}";

            // Write script file
            string scriptFullPath = Path.Combine(summary.ResultsDirectoryPath, scriptFileName);
            File.WriteAllText(scriptFullPath, script);

            // Find RScript executable
            if (!TryFindRScript(consoleLogger, out string rscriptPath))
            {
                throw new InvalidOperationException("RScript executable not found");
            }

            // Prepare process
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                FileName = rscriptPath,
                WorkingDirectory = summary.ResultsDirectoryPath,
                Arguments = $"\"{scriptFullPath}\""
            };

            // Execute and collect output
            using (var process = new Process { StartInfo = startInfo })
            {
                var output = new List<string>();
                var outputDone = new ManualResetEventSlim();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        outputDone.Set();
                    else if (e.Data.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        output.Add(e.Data.Trim());
                };

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                outputDone.Wait();

                // Convert relative paths to full paths
                pngPaths.AddRange(output.Select(p => Path.Combine(summary.ResultsDirectoryPath, p)));

                outputDone.Dispose();
            }

            return pngPaths;
        }
    }
}
