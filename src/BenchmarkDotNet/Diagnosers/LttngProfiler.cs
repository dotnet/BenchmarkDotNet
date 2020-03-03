using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class LttngProfiler : IProfiler
    {
        private const int SuccesExitCode = 0;
        private const string PerfCollectFileName = "perfcollect";
        public static readonly IDiagnoser Default = new LttngProfiler(new LttngProfilerConfig(performExtraBenchmarksRun: false));

        private readonly LttngProfilerConfig config;
        private readonly DateTime creationTime = DateTime.Now;
        private readonly Dictionary<BenchmarkCase, FileInfo> benchmarkToTraceFile = new Dictionary<BenchmarkCase, FileInfo>();

        private Process perfCollectProcess;
        private ManualResetEventSlim signal = new ManualResetEventSlim();

        [PublicAPI]
        public LttngProfiler(LttngProfilerConfig config) => this.config = config;

        public string ShortName => "LTTng";

        public IEnumerable<string> Ids => new[] { nameof(LttngProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => Array.Empty<Metric>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => config.RunMode;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            if (!RuntimeInformation.IsLinux())
            {
                yield return new ValidationError(true, "The LttngProfiler works only on Linux!");
                yield break;
            }

            if (Mono.Unix.Native.Syscall.getuid() != 0)
            {
                yield return new ValidationError(true, "You must run as root to use LttngProfiler.");
                yield break;
            }

            if (validationParameters.Benchmarks.Any() && !TryInstallPerfCollect(validationParameters))
            {
                yield return new ValidationError(true, "Failed to install perfcollect script. Please follow the instructions from https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/linux-performance-tracing.md#preparing-your-machine");
            }
        }

        public void DisplayResults(ILogger logger)
        {
            if (!benchmarkToTraceFile.Any())
                return;

            logger.WriteLineInfo($"Exported {benchmarkToTraceFile.Count} trace file(s). Example:");
            logger.WriteLineInfo(benchmarkToTraceFile.Values.First().FullName);
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            // it's crucial to start the trace before the process starts and stop it after the benchmarked process stops to have all of the necessary events in the trace file!
            if (signal == HostSignal.BeforeAnythingElse)
                Start(parameters);
            else if (signal == HostSignal.AfterProcessExit)
                Stop(parameters);
        }

        private bool TryInstallPerfCollect(ValidationParameters validationParameters)
        {
            var scriptInstallationDirectory = new DirectoryInfo(validationParameters.Config.ArtifactsPath).CreateIfNotExists();

            var perfCollectFile = scriptInstallationDirectory.GetFiles(PerfCollectFileName).SingleOrDefault();
            if (perfCollectFile != default)
            {
                return true;
            }

            var logger = validationParameters.Config.GetCompositeLogger();
            perfCollectFile = new FileInfo(Path.Combine(scriptInstallationDirectory.FullName, PerfCollectFileName));
            using (var client = new WebClient())
            {
                logger.WriteLineInfo($"// Downloading perfcollect: {perfCollectFile.FullName}");
                client.DownloadFile("https://aka.ms/perfcollect", perfCollectFile.FullName);
            }

            if (Mono.Unix.Native.Syscall.chmod(perfCollectFile.FullName, Mono.Unix.Native.FilePermissions.S_IXUSR) != SuccesExitCode)
            {
                logger.WriteError($"Unable to make perfcollect script an executable, the last error was: {Mono.Unix.Native.Syscall.GetLastError()}");
            }
            else
            {
                (int exitCode, var output) = ProcessHelper.RunAndReadOutputLineByLine(perfCollectFile.FullName, "install", perfCollectFile.Directory.FullName, null, includeErrors: true, logger);

                if (exitCode == SuccesExitCode)
                {
                    return true;
                }

                logger.WriteLineError("Failed to install perfcollect");
                foreach(var outputLine in output)
                {
                    logger.WriteLine(outputLine);
                }
            }

            if (perfCollectFile.Exists)
            {
                perfCollectFile.Delete(); // if the file exists it means that perfcollect is installed
            }

            return false;
        }

        private void Start(DiagnoserActionParameters parameters)
        {
            var perfCollectFile = new FileInfo(Directory.GetFiles(parameters.Config.ArtifactsPath, PerfCollectFileName).Single());

            perfCollectProcess = CreatePerfCollectProcess(parameters, perfCollectFile);

            var logger = parameters.Config.GetCompositeLogger();

            perfCollectProcess.OutputDataReceived += OnOutputDataReceived;

            signal.Reset();

            perfCollectProcess.Start();
            perfCollectProcess.BeginOutputReadLine();

            WaitForSignal(logger, "// Collection with perfcollect started"); // wait until the script starts the actual collection
        }

        private void Stop(DiagnoserActionParameters parameters)
        {
            if (perfCollectProcess == null)
            {
                return;
            }

            var logger = parameters.Config.GetCompositeLogger();

            if (WaitForSignal(logger, "// Collection with perfcollect stopped"))
            {
                benchmarkToTraceFile[parameters.BenchmarkCase] = new FileInfo(ArtifactFileNameHelper.GetFilePath(parameters, creationTime, ".trace.zip"));

                CleanupPerfCollectProcess(logger);
            }
        }

        private Process CreatePerfCollectProcess(DiagnoserActionParameters parameters, FileInfo perfCollectFile)
        {
            var traceName = new FileInfo(ArtifactFileNameHelper.GetFilePath(parameters, creationTime, fileExtension: null)).Name;
            // todo: escape characters bash does not like ' ', '(' etc

            var start = new ProcessStartInfo
            {
                FileName = perfCollectFile.FullName,
                Arguments = $"collect {traceName} -pid {parameters.Process.Id}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = perfCollectFile.Directory.FullName
            };

            return new Process { StartInfo = start };
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data?.IndexOf("Collection started", StringComparison.OrdinalIgnoreCase) >= 0)
                signal.Set();
            else if (e.Data?.IndexOf("Trace saved", StringComparison.OrdinalIgnoreCase) >= 0)
                signal.Set();
            else if (e.Data?.IndexOf("This script must be run as root", StringComparison.OrdinalIgnoreCase) >= 0)
                Environment.FailFast("To use LttngProfiler you must run as root."); // should never happen, ensured by Validate()
        }

        private bool WaitForSignal(ILogger logger, string message)
        {
            if (signal.Wait(config.Timeout))
            {
                signal.Reset();

                logger.WriteLineInfo(message);

                return true;
            }

            logger.WriteLineError($"The perfcollect script did not start/finish in {config.Timeout.TotalSeconds}s.");
            logger.WriteLineInfo("You can create LttngProfiler providing LttngProfilerConfig with custom timeout value.");

            CleanupPerfCollectProcess(logger);

            return false;
        }

        private void CleanupPerfCollectProcess(ILogger logger)
        {
            logger.Flush(); // flush recently logged message to disk

            try
            {
                perfCollectProcess.OutputDataReceived -= OnOutputDataReceived;

                if (!perfCollectProcess.HasExited)
                {
                    perfCollectProcess.KillTree(); // kill the entire process tree
                }
            }
            finally
            {
                perfCollectProcess.Dispose();
                perfCollectProcess = null;
            }
        }
    }

    public class LttngProfilerConfig
    {
        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. True by default.</param>
        /// <param name="timeoutInSeconds">how long should we wait for the perfcollect script to start collecting and/or finish processing the trace. 30s by default</param>
        public LttngProfilerConfig(bool performExtraBenchmarksRun = true, int timeoutInSeconds = 60)
        {
            RunMode = performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            Timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        }

        public TimeSpan Timeout { get; }

        public RunMode RunMode { get; }
    }
}
