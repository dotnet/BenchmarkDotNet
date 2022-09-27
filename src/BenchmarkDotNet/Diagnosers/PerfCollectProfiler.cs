using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Mono.Unix.Native;

namespace BenchmarkDotNet.Diagnosers
{
    public class PerfCollectProfiler : IProfiler
    {
        private const int SuccessExitCode = 0;
        private const string PerfCollectFileName = "perfcollect";

        public static readonly IDiagnoser Default = new PerfCollectProfiler(new PerfCollectProfilerConfig(performExtraBenchmarksRun: false));

        private readonly PerfCollectProfilerConfig config;
        private readonly DateTime creationTime = DateTime.Now;
        private readonly Dictionary<BenchmarkCase, FileInfo> benchmarkToTraceFile = new ();
        private readonly HashSet<string> cliPathWithSymbolsInstalled = new ();
        private FileInfo perfCollectFile;
        private Process perfCollectProcess;

        [PublicAPI]
        public PerfCollectProfiler(PerfCollectProfilerConfig config) => this.config = config;

        public string ShortName => "PC";

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

            if (Syscall.getuid() != 0)
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

            perfCollectFile = scriptInstallationDirectory.GetFiles(PerfCollectFileName).SingleOrDefault();
            if (perfCollectFile != default)
            {
                return true;
            }

            var logger = validationParameters.Config.GetCompositeLogger();
            perfCollectFile = new FileInfo(Path.Combine(scriptInstallationDirectory.FullName, PerfCollectFileName));

            string script = ResourceHelper.LoadTemplate(PerfCollectFileName);
            File.WriteAllText(perfCollectFile.FullName, script);

            if (Syscall.chmod(perfCollectFile.FullName, FilePermissions.S_IXUSR) != SuccessExitCode)
            {
                logger.WriteError($"Unable to make perfcollect script an executable, the last error was: {Syscall.GetLastError()}");
            }
            else
            {
                (int exitCode, var output) = ProcessHelper.RunAndReadOutputLineByLine(perfCollectFile.FullName, "install -force", perfCollectFile.Directory.FullName, null, includeErrors: true, logger);

                if (exitCode == SuccessExitCode)
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
            EnsureDotnetSymbolIsInstalled(parameters);

            var traceName = new FileInfo(ArtifactFileNameHelper.GetTraceFilePath(parameters, creationTime, fileExtension: null)).Name;

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
                    if (Syscall.kill(perfCollectProcess.Id, Signum.SIGINT) != 0)
                    {
                        var lastError = Stdlib.GetLastError();
                        logger.WriteLineError($"kill(perfcollect, SIGINT) failed with {lastError}");
                    }

                    if (!perfCollectProcess.WaitForExit((int)config.Timeout.TotalMilliseconds))
                    {
                        logger.WriteLineError($"The perfcollect script did not stop in {config.Timeout.TotalSeconds}s. It's going to be force killed now.");
                        logger.WriteLineInfo("You can create PerfCollectProfiler providing PerfCollectProfilerConfig with custom timeout value.");

                        perfCollectProcess.KillTree(); // kill the entire process tree
                    }

                    FileInfo traceFile = new (ArtifactFileNameHelper.GetTraceFilePath(parameters, creationTime, "trace.zip"));
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

        private void EnsureDotnetSymbolIsInstalled(DiagnoserActionParameters parameters)
        {
            string cliPath = parameters.BenchmarkCase.GetToolchain() switch
            {
                CsProjCoreToolchain core => core.CustomDotNetCliPath,
                CoreRunToolchain coreRun => coreRun.CustomDotNetCliPath.FullName,
                NativeAotToolchain nativeAot => nativeAot.CustomDotNetCliPath,
                _ => null // custom toolchain, dotnet from $PATH will be used
            };

            if (cliPathWithSymbolsInstalled.Contains(cliPath))
            {
                return;
            }

            cliPathWithSymbolsInstalled.Add(cliPath);

            ILogger logger = parameters.Config.GetCompositeLogger();
            DotNetCliCommand cliCommand = new (
                cliPath: cliPath,
                arguments: "--info",
                generateResult: null,
                logger: logger,
                buildPartition: null,
                environmentVariables: Array.Empty<EnvironmentVariable>(),
                timeout: TimeSpan.FromMinutes(3),
                logOutput: false);

            var dotnetInfoResult = DotNetCliCommandExecutor.Execute(cliCommand);
            if (!dotnetInfoResult.IsSuccess)
            {
                logger.WriteError($"Unable to run `dotnet --info` for `{cliPath}`, dotnet symbol won't be installed");
                return;
            }

            // sth like "Microsoft.NETCore.App 7.0.0-rc.2.22451.11 [/home/adam/projects/performance/tools/dotnet/x64/shared/Microsoft.NETCore.App]"
            // or "Microsoft.NETCore.App 7.0.0-rc.1.22423.16 [/usr/share/dotnet/shared/Microsoft.NETCore.App]"
            string netCoreAppPath = dotnetInfoResult
                .StandardOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.EndsWith("Microsoft.NETCore.App]"))
                .Select(line => line.Split('[')[1])
                .Distinct()
                .Single(); // I assume there will be only one such folder
            netCoreAppPath = netCoreAppPath.Substring(0, netCoreAppPath.Length - 1); // remove trailing `]`

            string[] missingSymbols = Directory.GetFiles(netCoreAppPath, "lib*.so", SearchOption.AllDirectories)
                .Where(nativeLibPath => !File.Exists(Path.ChangeExtension(nativeLibPath, "so.dbg")))
                .Select(Path.GetDirectoryName)
                .Distinct()
                .ToArray();

            if (!missingSymbols.Any())
            {
                return; // the symbol files are already where we need them!
            }

            cliCommand = cliCommand.WithLogOutput(true); // the following commands might take a while and fail, let's log them

            // We install the tool in a dedicated directory in order to always use latest version and avoid issues with broken existing configs.
            string toolPath = Path.Combine(Path.GetTempPath(), "BenchmarkDotNet", "symbols");
            var installResult = DotNetCliCommandExecutor.Execute(cliCommand.WithArguments($"tool install dotnet-symbol --tool-path \"{toolPath}\""));
            if (!installResult.IsSuccess)
            {
                logger.WriteError($"Unable to install dotnet symbol.");
                return;
            }

            foreach (var directoryPath in missingSymbols)
            {
                DotNetCliCommandExecutor.Execute(cliCommand
                    .WithCliPath(Path.Combine(toolPath, "dotnet-symbol"))
                    .WithArguments($"--symbols --output {directoryPath} {directoryPath}/lib*.so"));
            }

            DotNetCliCommandExecutor.Execute(cliCommand.WithArguments($"tool uninstall dotnet-symbol --tool-path \"{toolPath}\""));
        }
    }
}