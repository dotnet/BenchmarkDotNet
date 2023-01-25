namespace BenchmarkDotNet.Diagnostics.Windows {
    using BenchmarkDotNet.Analysers;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Engines;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Loggers;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Validators;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class VSDiagnosticsProfiler : IProfiler {
        private VSDiagnosticsTool _config;
        private int _sessionId;
        private string _collectorPath;
        private string _outputFile;
        private bool _isStarted;

        public enum VSDiagnosticsTool {
            CpuUsageBase,
            CpuUsageHigh,
            CpuUsageLow,
            CpuUsageWithCallCounts,
            DatabaseBase,
            DotNetAsyncBase,
            DotNetCountersBase,
            DotNetObjectAllocBase,
            DotNetObjectAllocLow,
            EventsBase,
            FileIOBase,
            PerfInstrumentation,
        }

        public VSDiagnosticsProfiler(VSDiagnosticsTool config = VSDiagnosticsTool.CpuUsageBase, string pathToVSDiagnostics = null, string outputFile = null) {
            _config = config;
            _sessionId = new Random().Next() % 255;
            _collectorPath = LocateCollectorPath(pathToVSDiagnostics);
            _outputFile = outputFile ?? $"benchmark_{_sessionId}.diagsession";
        }

        public IEnumerable<string> Ids => new[] { nameof(VSDiagnosticsProfiler) };

        public string ShortName => "VSDiagnostics";

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) {
            if (signal == HostSignal.BeforeAnythingElse) {
                StartOrResume(parameters);
                Pause(parameters);
            }
            else if (signal == HostSignal.BeforeActualRun) {
                StartOrResume(parameters);
            }
            else if (signal == HostSignal.AfterActualRun) {
                Pause(parameters);
            }
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) {
            return Array.Empty<Metric>();
        }

        public void DisplayResults(ILogger logger) {
            Stop();
            logger.WriteLineInfo($"Exported diagsession file(s).");
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) {
            return Array.Empty<ValidationError>();
        }

        private void Stop() {
            RunVSDiagnosticsCommand($"stop {_sessionId} /output:{_outputFile}");
        }

        private bool RunVSDiagnosticsCommand(string arguments) {
            var pathToVSDiagnostics = Path.Combine(_collectorPath, "VSDiagnostics.exe");
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    WorkingDirectory = _collectorPath,
                    FileName = pathToVSDiagnostics,
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            bool success = process.Start();
            process.WaitForExit();
            return success;
        }

        private void Pause(DiagnoserActionParameters parameters) {
            RunVSDiagnosticsCommand($"pause {_sessionId}");
        }

        private void StartOrResume(DiagnoserActionParameters parameters) {
            var pidToAttach = parameters.Process.Id;
            var configFile = $"AgentConfigs\\{_config}.json";

            string arguments;
            if (_isStarted) {
                arguments = $"resume {_sessionId}";
            } else {
                arguments = $"start {_sessionId} /attach:{pidToAttach} /loadConfig:{configFile}";
            }

            RunVSDiagnosticsCommand(arguments);
            _isStarted = true;
        }

        private string LocateCollectorPath(string collectorPath = null) {
            if (File.Exists(collectorPath)) {
                return Path.GetDirectoryName(collectorPath);
            }

            throw new FileNotFoundException(nameof(collectorPath));
        }
    }
}
