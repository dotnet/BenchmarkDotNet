using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
        private const string PerfCollectFileName = "perfcollect";

        public static readonly IDiagnoser Default = new LttngProfiler(new LttngProfilerConfig(performExtraBenchmarksRun: false));

        private readonly LttngProfilerConfig config;
        private readonly DateTime creationTime = DateTime.Now;
        private readonly Dictionary<BenchmarkCase, FileInfo> benchmarkToTraceFile = new Dictionary<BenchmarkCase, FileInfo>();

        private Process perfCollectProcess;
        private ConsoleExitHandler consoleExitHandler;

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
            }
            if (validationParameters.Benchmarks.Any() && !TryInstallPerfCollect(validationParameters))
            {
                yield return new ValidationError(true, "Please run as sudo, it's required to use LttngProfiler.");
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
            if (signal == HostSignal.BeforeProcessStart)
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
                logger.WriteLineInfo($"downloading perfcollect: {perfCollectFile.FullName}");
                client.DownloadFile("https://aka.ms/perfcollect", perfCollectFile.FullName);
            }

            var processOutput = ProcessHelper.RunAndReadOutput("/bin/bash", $"-c \"sudo chmod +x {perfCollectFile.FullName}\"", logger);
            if (processOutput != null)
            {
                processOutput = ProcessHelper.RunAndReadOutput("/bin/bash", $"-c \"sudo {perfCollectFile.FullName} install\"", logger);
            }

            if (processOutput != null)
            {
                return true;
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

            consoleExitHandler = new ConsoleExitHandler(perfCollectProcess, parameters.Config.GetCompositeLogger());

            perfCollectProcess.Start();

            while(perfCollectProcess.StandardOutput.ReadLine()?.IndexOf("Collection started", StringComparison.OrdinalIgnoreCase) < 0)
            {
                // wait until the script starts the actual collection
            }
        }

        private Process CreatePerfCollectProcess(DiagnoserActionParameters parameters, FileInfo perfCollectFile)
        {
            var traceName = TraceFileHelper.GetFilePath(parameters.BenchmarkCase, parameters.Config, creationTime, fileExtension: null).Name;

            var start = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"sudo '{perfCollectFile.FullName}' collect '{traceName}'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = perfCollectFile.Directory.FullName
            };

            return new Process { StartInfo = start };
        }

        private void Stop(DiagnoserActionParameters parameters)
        {
            try
            {
                perfCollectProcess.StandardInput.Close(); // signal Ctrl + C to the script to tell it to stop profiling

                while (perfCollectProcess.StandardOutput.ReadLine()?.IndexOf("Trace saved", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    // wait until the script ends post-processing
                }

                if (!perfCollectProcess.HasExited && !perfCollectProcess.WaitForExit((int)config.TracePostProcessingTimeout.TotalMilliseconds))
                {
                    var logger = parameters.Config.GetCompositeLogger();
                    logger.WriteLineError($"The perfcollect script did not finish the post processing in {config.TracePostProcessingTimeout.TotalSeconds}s.");
                    logger.WriteLineInfo("You can create LttngProfiler providing LttngProfilerConfig with custom timeout value.");

                    perfCollectProcess.KillTree();
                }

                if (perfCollectProcess.HasExited && perfCollectProcess.ExitCode == 0)
                {
                    benchmarkToTraceFile[parameters.BenchmarkCase] = TraceFileHelper.GetFilePath(parameters.BenchmarkCase, parameters.Config, creationTime, ".trace.zip");
                }
            }
            finally
            {
                consoleExitHandler.Dispose();
                consoleExitHandler = null;
                perfCollectProcess.Dispose();
                perfCollectProcess = null;
            }
        }
    }

    public class LttngProfilerConfig
    {
        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. True by default.</param>
        /// <param name="tracePostProcessingTimeoutInSeconds">how long should we wait for the perfcollect script to finish processing trace. 30s by default</param>
        public LttngProfilerConfig(bool performExtraBenchmarksRun = true, int tracePostProcessingTimeoutInSeconds = 30)
        {
            RunMode = performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            TracePostProcessingTimeout = TimeSpan.FromSeconds(tracePostProcessingTimeoutInSeconds);
        }

        public TimeSpan TracePostProcessingTimeout { get; }

        public RunMode RunMode { get; }
    }
}
