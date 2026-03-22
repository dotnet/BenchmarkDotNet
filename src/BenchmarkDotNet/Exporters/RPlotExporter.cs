using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.Parameters;

namespace BenchmarkDotNet.Exporters
{
    public class RPlotExporter : IExporter, IExporterDependencies
    {
        public static readonly IExporter Default = new RPlotExporter();
        public string Name => nameof(RPlotExporter);

        private const string ImageExtension = ".png";
        private static readonly SemaphoreSlim BuildScriptLock = new(1, 1);

        public IEnumerable<IExporter> Dependencies
        {
            // R Plots depends on having the full measurements available
            get { yield return CsvMeasurementsExporter.Default; }
        }

        public async ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
        {
            const string scriptFileName = "BuildPlots.R";
            const string logFileName = "BuildPlots.log";

            string csvFullPath = Path.GetFullPath(CsvMeasurementsExporter.Default.GetArtifactFullName(summary));
            string scriptFullPath = Path.GetFullPath(Path.Combine(summary.ResultsDirectoryPath, scriptFileName));
            string logFullPath = Path.GetFullPath(Path.Combine(summary.ResultsDirectoryPath, logFileName));

            logger.WriteLineInfo($"  {scriptFullPath.GetBaseName(Directory.GetCurrentDirectory())}");

            string script = (await ResourceHelper.LoadTemplateAsync(scriptFileName, cancellationToken).ConfigureAwait(false))
                .Replace("$BenchmarkDotNetVersion$", BenchmarkDotNetInfo.Instance.BrandTitle)
                .Replace("$CsvSeparator$", CsvMeasurementsExporter.Default.Separator);
            using (await BuildScriptLock.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                await File.WriteAllTextAsync(scriptFullPath, script, cancellationToken).ConfigureAwait(false);
            }

            if (!TryFindRScript(logger, out var rscriptPath))
            {
                return;
            }

            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                FileName = rscriptPath,
                Arguments = $"\"{scriptFullPath}\" \"{csvFullPath}\""
            };
            using (var process = new Process { StartInfo = start })
            using (AsyncProcessOutputReader reader = new(process, cacheStandardOutput: false, channelStandardOutput: true))
            using (ProcessCleanupHelper processCleanupHelper = new(process, logger))
            {
                // When large R scripts are generated then ran, ReadToEnd()
                // causes the stdout and stderr buffers to become full,
                // which causes R to hang. To avoid this, use
                // AsyncProcessOutputReader to stream the log contents
                // to disk rather than Process.Standard*.ReadToEnd().
                process.Start();
                using var fileStream = new FileStream(logFullPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
                using var streamWriter = new CancelableStreamWriter(fileStream);
                try
                {
                    reader.BeginRead();

                    await foreach (var line in reader.OutputChannel!.Reader.ReadAllAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        await streamWriter.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (!process.WaitForExit(milliseconds: (int) ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                    {
                        reader.CancelRead();
                        processCleanupHelper.KillProcessTree();
                    }
                    else
                    {
                        await reader.StopReadAsync().ConfigureAwait(false);
                    }
                }

                foreach (var line in reader.GetErrorLines())
                {
                    await streamWriter.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
                }

                Debug.Assert(process.HasExited);
                if (process.ExitCode != 0)
                    throw new ApplicationException($"Process {rscriptPath} has exited with code {process.ExitCode}");
            }

            logger.WriteLineInfo($"  {Path.Combine(summary.ResultsDirectoryPath, $"*{ImageExtension}").GetBaseName(Directory.GetCurrentDirectory())}");
        }

        private static bool TryFindRScript(ILogger consoleLogger, out string? rscriptPath)
        {
            string rscriptExecutable = OsDetector.IsWindows() ? "Rscript.exe" : "Rscript";
            rscriptPath = null;

            string? rHome = Environment.GetEnvironmentVariable("R_HOME");
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
            string? path = Environment.GetEnvironmentVariable("PATH");
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
    }
}
