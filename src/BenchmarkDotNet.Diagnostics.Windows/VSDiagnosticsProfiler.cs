namespace BenchmarkDotNet.Diagnostics.Windows {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BenchmarkDotNet.Analysers;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Engines;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Loggers;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Validators;

    public class VSDiagnosticsProfiler : IProfiler {
        private readonly VSDiagnosticsTool _config;
        private readonly int _sessionId;
        private readonly string _collectorPath;
        private readonly string _pathToVSDiagnostics;
        private readonly string _outputFile;
        private State _state;
        private int _currentProcessId;

        /// <summary>
        /// Agent configs.
        /// see https://learn.microsoft.com/en-us/visualstudio/profiling/profile-apps-from-command-line?view=vs-2022#config_file
        /// </summary>
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

        /// <summary>
        /// Run VSDiagnostics.exe to collect a diagsession
        /// </summary>
        /// <param name="outputFile">Path to the output file. If null, the output will be benchmark_*.diagsession</param>
        /// <param name="config">VSDiagnostics Agent Configuration, see <see cref="VSDiagnosticsTool"/></param>
        /// <param name="pathToVSDiagnostics">Path to VSDiagnostics.exe. If null, search in common VS Install locations.</param>
        public VSDiagnosticsProfiler(string outputFile = null, VSDiagnosticsTool config = VSDiagnosticsTool.CpuUsageBase, string pathToVSDiagnostics = null) {
            _config = config;
            _sessionId = new Random().Next() % 255;
            _collectorPath = LocateCollectorPath(pathToVSDiagnostics);
            _pathToVSDiagnostics = Path.Combine(_collectorPath, "VSDiagnostics.exe");
            _outputFile = outputFile ?? $"benchmark_{_sessionId}.diagsession";
            _state = State.New;
            _currentProcessId = 0;
        }

        /// <inheritdoc/>
        public IEnumerable<string> Ids => new[] { nameof(VSDiagnosticsProfiler) };

        /// <inheritdoc/>
        public string ShortName => "VSDiagnostics";

        /// <inheritdoc/>
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        /// <inheritdoc/>
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        /// <inheritdoc/>
        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) {
            return Array.Empty<Metric>();
        }

        /// <inheritdoc/>
        public void DisplayResults(ILogger logger) {
            RunVSDiagnosticsCommand($"stop {_sessionId} /output:{_outputFile}");
            _state = State.Stopped;

            logger.WriteLineInfo($"Exported diagsession file: {_outputFile}.");
        }

        /// <inheritdoc/>
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) {
            return Array.Empty<ValidationError>();
        }

        private bool RunVSDiagnosticsCommand(string arguments) {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    WorkingDirectory = _collectorPath,
                    FileName = _pathToVSDiagnostics,
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

        /// <summary>
        /// Locate the path of Collector, usually ${VS_INSTALL_DIR}\Team Tools\DiagnosticsHub\Collector
        /// If path hint is supplied, first check if we can use it. If not, search in well known locations.
        /// </summary>
        private string LocateCollectorPath(string collectorPathHint = null) {
            if (File.Exists(collectorPathHint)) {
                return Path.GetDirectoryName(collectorPathHint);
            }

            // 1. Get the year folders under C:\Program Files (x86)\Microsoft Visual Studio\* and C:\Program Files\Microsoft Visual Studio\*
            var vsYearDirs = new List<string>();
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            Regex year = new (@"\\\d\d\d\d$");

            if (Directory.Exists(programFilesX86)) {
                vsYearDirs.AddRange(Directory.GetDirectories(programFilesX86, "Microsoft Visual Studio\\*").Where(i => year.IsMatch(i)));
            }
            if (Directory.Exists(programFiles)) {
                vsYearDirs.AddRange(Directory.GetDirectories(programFiles, "Microsoft Visual Studio\\*").Where(i => year.IsMatch(i)));
            }

            // 2. Get the latest version folder under each year folder
            var vsInstallDirs = new List<string>();
            foreach (var vsInstallDir in vsYearDirs) {
                vsInstallDirs.AddRange(Directory.GetDirectories(vsInstallDir));
            }

            // 3. Return the first ${VS_INSTALL_DIR}\Team Tools\DiagnosticsHub\Collector
            foreach (var vsInstallDir in vsInstallDirs) {
                var collectorPath = Path.Combine(vsInstallDir, "Team Tools", "DiagnosticsHub", "Collector");
                if (File.Exists(Path.Combine(collectorPath, "VSDiagnostics.exe"))) {
                    return collectorPath;
                }
            }

            throw new FileNotFoundException(nameof(collectorPathHint));
        }
    }
}
