using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using RuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Diagnosers
{
    public class PerfCollectProfiler : IProfiler
    {
        public static readonly IDiagnoser Default = new PerfCollectProfiler(new PerfCollectProfilerConfig(performExtraBenchmarksRun: false));

        private readonly PerfCollectProfilerConfig config;
        private readonly DateTime creationTime = DateTime.Now;
        private readonly Dictionary<BenchmarkCase, FileInfo> benchmarkToTraceFile = new ();
        private readonly HashSet<string> cliPathWithSymbolsInstalled = new ();
        private FileInfo perfCollectFile;
        private Process perfCollectProcess;

        [PublicAPI]
        public PerfCollectProfiler(PerfCollectProfilerConfig config) => this.config = config;

        public string ShortName => "perf";

        public IEnumerable<string> Ids => new[] { nameof(PerfCollectProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => Array.Empty<Metric>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => config.RunMode;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            if (!RuntimeInformation.IsLinux())
            {
                yield return new ValidationError(true, "The PerfCollectProfiler works only on Linux!");
                yield break;
            }

            if (libc.getuid() != 0)
            {
                yield return new ValidationError(true, "You must run as root to use PerfCollectProfiler.");
                yield break;
            }

            if (validationParameters.Benchmarks.Any() && !TryInstallPerfCollect(validationParameters))
            {
                yield return new ValidationError(true, "Failed to install perfcollect script. Please follow the instructions from https://github.com/dotnet/runtime/blob/main/docs/project/linux-performance-tracing.md");
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
            if (signal == HostSignal.BeforeProcessStart)
                perfCollectProcess = StartCollection(parameters);
            else if (signal == HostSignal.AfterProcessExit)
                StopCollection(parameters);
        }

        private bool TryInstallPerfCollect(ValidationParameters validationParameters)
        {
            var scriptInstallationDirectory = new DirectoryInfo(validationParameters.Config.ArtifactsPath).CreateIfNotExists();

            perfCollectFile = new FileInfo(Path.Combine(scriptInstallationDirectory.FullName, "perfcollect"));
            if (perfCollectFile.Exists)
            {
                return true;
            }

            var logger = validationParameters.Config.GetCompositeLogger();

            string script = ResourceHelper.LoadTemplate(perfCollectFile.Name);
            File.WriteAllText(perfCollectFile.FullName, script);

            if (libc.chmod(perfCollectFile.FullName, libc.FilePermissions.S_IXUSR) != 0)
            {
                int lastError = Marshal.GetLastWin32Error();
                logger.WriteError($"Unable to make perfcollect script an executable, the last error was: {lastError}");
            }
            else
            {
                (int exitCode, var output) = ProcessHelper.RunAndReadOutputLineByLine(perfCollectFile.FullName, "install -force", perfCollectFile.Directory.FullName, null, includeErrors: true, logger);

                if (exitCode == 0)
                {
                    logger.WriteLine("Successfully installed perfcollect");
                    return true;
                }

                logger.WriteLineError("Failed to install perfcollect");
                foreach (var outputLine in output)
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

        private Process StartCollection(DiagnoserActionParameters parameters)
        {
            EnsureSymbolsForNativeRuntime(parameters);

            var traceName = GetTraceFile(parameters, extension: null).Name;

            var start = new ProcessStartInfo
            {
                FileName = perfCollectFile.FullName,
                Arguments = $"collect \"{traceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = perfCollectFile.Directory.FullName
            };

            return Process.Start(start);
        }

        private void StopCollection(DiagnoserActionParameters parameters)
        {
            var logger = parameters.Config.GetCompositeLogger();

            try
            {
                if (!perfCollectProcess.HasExited)
                {
                    if (libc.kill(perfCollectProcess.Id, libc.Signals.SIGINT) != 0)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        logger.WriteLineError($"kill(perfcollect, SIGINT) failed with {lastError}");
                    }

                    if (!perfCollectProcess.WaitForExit((int)config.Timeout.TotalMilliseconds))
                    {
                        logger.WriteLineError($"The perfcollect script did not stop in {config.Timeout.TotalSeconds}s. It's going to be force killed now.");
                        logger.WriteLineInfo("You can create PerfCollectProfiler providing PerfCollectProfilerConfig with custom timeout value.");

                        perfCollectProcess.KillTree(); // kill the entire process tree
                    }

                    FileInfo traceFile = GetTraceFile(parameters, "trace.zip");
                    if (traceFile.Exists)
                    {
                        benchmarkToTraceFile[parameters.BenchmarkCase] = traceFile;
                    }
                }
                else
                {
                    logger.WriteLineError("For some reason the perfcollect script has finished sooner than expected.");
                    logger.WriteLineInfo($"Please run '{perfCollectFile.FullName} install' as root and re-try.");
                }
            }
            finally
            {
                perfCollectProcess.Dispose();
            }
        }

        private void EnsureSymbolsForNativeRuntime(DiagnoserActionParameters parameters)
        {
            if (parameters.BenchmarkCase.GetToolchain() is CoreRunToolchain)
            {
                return; // it's not needed for a local build of dotnet runtime
            }

            string cliPath = parameters.BenchmarkCase.GetToolchain() switch
            {
                CsProjCoreToolchain core => core.CustomDotNetCliPath,
                NativeAotToolchain nativeAot => nativeAot.CustomDotNetCliPath,
                _ => DotNetCliCommandExecutor.DefaultDotNetCliPath.Value
            };

            if (!cliPathWithSymbolsInstalled.Add(cliPath))
            {
                return;
            }

            string sdkPath = DotNetCliCommandExecutor.GetSdkPath(cliPath); // /usr/share/dotnet/sdk/
            string dotnetPath = Path.GetDirectoryName(sdkPath); // /usr/share/dotnet/
            string[] missingSymbols = Directory.GetFiles(dotnetPath, "lib*.so", SearchOption.AllDirectories)
                .Where(nativeLibPath => !nativeLibPath.Contains("FallbackFolder") && !File.Exists(Path.ChangeExtension(nativeLibPath, "so.dbg")))
                .Select(Path.GetDirectoryName)
                .Distinct()
                .ToArray();

            if (!missingSymbols.Any())
            {
                return; // the symbol files are already where we need them!
            }

            ILogger logger = parameters.Config.GetCompositeLogger();
            // We install the tool in a dedicated directory in order to always use latest version and avoid issues with broken existing configs.
            string toolPath = Path.Combine(Path.GetTempPath(), "BenchmarkDotNet", "symbols");
            DotNetCliCommand cliCommand = new (
                cliPath: cliPath,
                arguments: $"tool install dotnet-symbol --tool-path \"{toolPath}\"",
                generateResult: null,
                logger: logger,
                buildPartition: null,
                environmentVariables: Array.Empty<EnvironmentVariable>(),
                timeout: TimeSpan.FromMinutes(3),
                logOutput: true); // the following commands might take a while and fail, let's log them

            var installResult = DotNetCliCommandExecutor.Execute(cliCommand);
            if (!installResult.IsSuccess)
            {
                logger.WriteError("Unable to install dotnet symbol.");
                return;
            }

            DotNetCliCommandExecutor.Execute(cliCommand
                .WithCliPath(Path.Combine(toolPath, "dotnet-symbol"))
                .WithArguments($"--recurse-subdirectories --symbols \"{dotnetPath}/dotnet\" \"{dotnetPath}/lib*.so\""));

            DotNetCliCommandExecutor.Execute(cliCommand.WithArguments($"tool uninstall dotnet-symbol --tool-path \"{toolPath}\""));
        }

        private FileInfo GetTraceFile(DiagnoserActionParameters parameters, string extension)
            => new (ArtifactFileNameHelper.GetTraceFilePath(parameters, creationTime, extension)
                    .Replace(" ", "_")); // perfcollect does not allow for spaces in the trace file name
    }
}
