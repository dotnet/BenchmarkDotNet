using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class ClassicExecutor : IExecutor
    {
        // This needs to be static, so that we can share a single handler amongst all instances of BenchmarkClassicExecutor's
        private static ConsoleHandler consoleHandler;

        public virtual ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IDiagnoser compositeDiagnoser = null)
        {
            var exePath = Path.Combine(buildResult.DirectoryPath, "Program.exe");
            var args = string.Empty;

            if (!File.Exists(exePath))
            {
                return new ExecuteResult(false, new string[0]);
            }

            return Execute(benchmark, logger, exePath, null, args, compositeDiagnoser);
        }

        protected ExecuteResult Execute(Benchmark benchmark, ILogger logger, string exeName, string workingDirectory, string args, IDiagnoser diagnoser)
        {
            if (consoleHandler == null)
            {
                consoleHandler = new ConsoleHandler(logger);
                Console.CancelKeyPress += consoleHandler.EventHandler;
            }

            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(benchmark, exeName, args, workingDirectory) })
                using (var safeLoger = new ProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark))
                {
                    return Execute(process, benchmark, safeLoger, diagnoser);
                }
            }
            finally
            {
                consoleHandler?.ClearProcess();
            }
        }

        private ExecuteResult Execute(Process process, Benchmark benchmark, ProcessOutputLoggerWithDiagnoser safeLoger, IDiagnoser compositeDiagnoser)
        {
            consoleHandler.SetProcess(process);

            process.Start();

            compositeDiagnoser?.ProcessStarted(process);

            process.EnsureHighPriority();
            if (!benchmark.Job.Affinity.IsAuto)
            {
                process.EnsureProcessorAffinity(benchmark.Job.Affinity.Value);
            }

            // don't forget to call, otherwise logger will not get any events
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit(); // should we add timeout here?

            compositeDiagnoser?.ProcessStopped(process);

            if (process.ExitCode == 0)
            {
                return new ExecuteResult(true, safeLoger.Lines);
            }

            return new ExecuteResult(true, new string[0]);
        }

        private ProcessStartInfo CreateStartInfo(Benchmark benchmark, string exeName, string args, string workingDirectory)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            var runtime = benchmark.Job.Runtime == Runtime.Host ? EnvironmentHelper.GetCurrentRuntime() : benchmark.Job.Runtime;
            switch (runtime)
            {
                case Runtime.Clr:
                    start.FileName = exeName;
                    start.Arguments = args;
                    break;
                case Runtime.Mono:
                    start.FileName = "mono";
                    start.Arguments = exeName + " " + args;
                    break;
                default:
                    throw new NotSupportedException("Runtime = " + benchmark.Job.Runtime);
            }
            return start;
        }

        private class ConsoleHandler
        {
            public ConsoleCancelEventHandler EventHandler { get; private set; }

            private Process process;
            private ILogger logger;

            public ConsoleHandler(ILogger logger)
            {
                this.logger = logger;
                EventHandler = new ConsoleCancelEventHandler(HandlerCallback);
            }

            public void SetProcess(Process process)
            {
                this.process = process;
            }

            public void ClearProcess()
            {
                this.process = null;
            }

            // This method gives us a chance to make a "best-effort" to clean anything up after Ctrl-C is type in the Console
            private void HandlerCallback(object sender, ConsoleCancelEventArgs e)
            {
                if (e.SpecialKey != ConsoleSpecialKey.ControlC && e.SpecialKey != ConsoleSpecialKey.ControlBreak)
                    return;

                try
                {
                    // Take a copy, in case SetProcess(..) is called whilst we are executing!
                    var localProcess = process;

                    if (HasProcessDied(localProcess))
                        return;

                    logger?.WriteLineError($"Process {localProcess.ProcessName}.exe (Id:{localProcess.Id}) is still running, will now be killed");
                    localProcess.Kill();

                    if (HasProcessDied(localProcess))
                        return;

                    // Give it a bit of time to exit!
                    Thread.Sleep(500);

                    if (HasProcessDied(localProcess))
                        return;

                    var matchingProcess = Process.GetProcesses().FirstOrDefault(p => p.Id == localProcess.Id);
                    if (HasProcessDied(matchingProcess) || HasProcessDied(localProcess))
                        return;
                    logger?.WriteLineError($"Process {matchingProcess.ProcessName}.exe (Id:{matchingProcess.Id}) has not exited after being killed!");
                }
                catch (InvalidOperationException invalidOpEx)
                {
                    logger?.WriteLineError(invalidOpEx.Message);
                }
                catch (Exception ex)
                {
                    logger?.WriteLineError(ex.ToString());
                }
            }

            public override string ToString()
            {
                int? processId = -1;
                try
                {
                    processId = process?.Id;
                }
                catch (Exception)
                {
                    // Swallow!!
                }
                return $"Process: {processId}, Handler: {EventHandler?.GetHashCode()}";
            }

            private bool HasProcessDied(Process process)
            {
                if (process == null)
                    return true;
                try
                {
                    return process.HasExited; // This can throw an exception
                }
                catch (Exception)
                {
                    // Swallow!!
                }
                return true;
            }
        }
    }
}