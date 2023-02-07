namespace BenchmarkDotNet.Diagnostics.Windows {
    using BenchmarkDotNet.Analysers;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Engines;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Loggers;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Validators;
    using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class VSDiagnosticsProfiler : IProfiler {
        private VSDiagnosticsTool _config;
        private int _sessionId;
        private string _collectorPath;
        private string _outputFile;
        private State _state;
        private int _currentProcessId;

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

        private enum State
        {
            New,
            RunningAttachedToPid,
            Paused,
            Stopped,
        }

        public VSDiagnosticsProfiler(VSDiagnosticsTool config = VSDiagnosticsTool.CpuUsageBase, string pathToVSDiagnostics = null, string outputFile = null) {
            _config = config;
            _sessionId = new Random().Next() % 255;
            _collectorPath = LocateCollectorPath(pathToVSDiagnostics);
            _outputFile = outputFile ?? $"benchmark_{_sessionId}.diagsession";
            _state = State.New;
            _currentProcessId = 0;
        }

        public IEnumerable<string> Ids => new[] { nameof(VSDiagnosticsProfiler) };

        public string ShortName => "VSDiagnostics";

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) {
            if (signal == HostSignal.BeforeActualRun) {
                switch (_state) {
                    case State.New:
                        var currentPid = parameters.Process?.Id ?? 0;
                        var configFile = $"AgentConfigs\\{_config}.json";
                        RunVSDiagnosticsCommand($"start {_sessionId} /attach:{currentPid} /loadConfig:{configFile}");
                        _currentProcessId = currentPid;
                        _state = State.RunningAttachedToPid;
                        break;
                    case State.RunningAttachedToPid:
                        break;
                    case State.Paused:
                        currentPid = parameters.Process?.Id ?? 0;
                        if (_currentProcessId != currentPid) {
                            RunVSDiagnosticsCommand($"update {_sessionId} /attach:{currentPid} /detach:{_currentProcessId}");
                        }

                        _currentProcessId = currentPid;
                        RunVSDiagnosticsCommand($"resume {_sessionId}");
                        _state = State.RunningAttachedToPid;
                        break;
                    case State.Stopped:
                        break;
                }
            }
            else if (signal == HostSignal.AfterActualRun) {
                RunVSDiagnosticsCommand($"pause {_sessionId}");
                _state = State.Paused;
            }
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) {
            return Array.Empty<Metric>();
        }

        public void DisplayResults(ILogger logger) {
            RunVSDiagnosticsCommand($"stop {_sessionId} /output:{_outputFile}");
            _state = State.Stopped;

            logger.WriteLineInfo($"Exported diagsession file: {_outputFile}.");
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) {
            return Array.Empty<ValidationError>();
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

        private string LocateCollectorPath(string collectorPath = null) {
            if (File.Exists(collectorPath)) {
                return Path.GetDirectoryName(collectorPath);
            }

            throw new FileNotFoundException(nameof(collectorPath));
        }
    }
}
