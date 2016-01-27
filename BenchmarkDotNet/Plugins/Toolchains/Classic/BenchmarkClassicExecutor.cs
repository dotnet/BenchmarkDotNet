using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains.Results;

namespace BenchmarkDotNet.Plugins.Toolchains.Classic
{
    internal class BenchmarkClassicExecutor : IBenchmarkExecutor
    {
        private readonly Benchmark benchmark;
        private readonly IBenchmarkLogger logger;
        private bool codeAlreadyExtracted = false;

        // This needs to be static, so that we can share a single handler amongst all instances of BenchmarkClassicExecutor's
        private static ConsoleHandler consoleHandler;

        public BenchmarkClassicExecutor(Benchmark benchmark, IBenchmarkLogger logger)
        {
            this.benchmark = benchmark;
            this.logger = logger;

            if (consoleHandler == null)
            {
                consoleHandler = new ConsoleHandler(logger);
                Console.CancelKeyPress += consoleHandler.EventHandler;
            }
        }

        public virtual BenchmarkExecResult Execute(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser)
        {
            var exeName = Path.Combine(buildResult.DirectoryPath, "Program.exe");
            var args = parameters == null ? string.Empty : parameters.ToArgs();

            if (!File.Exists(exeName))
            {
                return new BenchmarkExecResult(false, new string[0]);
            }

            return Execute(exeName, args, null, diagnoser);
        }

        protected BenchmarkExecResult Execute(string exeName, string workingDirectory, string args, IBenchmarkDiagnoser diagnoser)
        {
            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(exeName, args, workingDirectory) })
                using (var safeLoger = new ProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark))
                {
                    return Execute(process, safeLoger, exeName);
                }
            }
            finally
            {
                consoleHandler?.ClearProcess();
            }
        }

        private BenchmarkExecResult Execute(Process process, ProcessOutputLoggerWithDiagnoser safeLoger, string exeName)
        {
            consoleHandler.SetProcess(process);

            process.Start();

            process.PriorityClass = ProcessPriorityClass.High;
            process.ProcessorAffinity = new IntPtr(2);

            // don't forget to call, otherwise logger will not get any events
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit(); // should we add timeout here?

            if (process.ExitCode == 0)
            {
                return new BenchmarkExecResult(true, safeLoger.Lines);
            }

            if (logger != null)
            {
                logger.WriteError(
                    $"Something bad happened during the execution of {exeName}. Try to run the benchmark again using an AnyCPU application\n");
            }
            else
            {
                if (exeName.ToLowerInvariant() == "msbuild")
                {
                    Console.WriteLine("Build failed");
                }
            }

            return new BenchmarkExecResult(true, new string[0]);
        }

        private ProcessStartInfo CreateStartInfo(string exeName, string args, string workingDirectory)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            switch (benchmark.Task.Configuration.Runtime)
            {
                case BenchmarkRuntime.Clr:
                    start.FileName = exeName;
                    start.Arguments = args;
                    break;
                case BenchmarkRuntime.Mono:
                    start.FileName = "mono";
                    start.Arguments = exeName + " " + args;
                    break;
            }
            return start;
        }

        private class ConsoleHandler
        {
            public ConsoleCancelEventHandler EventHandler { get; private set; }

            private Process process;
            private IBenchmarkLogger logger;

            public ConsoleHandler(IBenchmarkLogger logger)
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