using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class ClassicExecutor : IExecutor
    {
        private bool codeAlreadyExtracted = false;

        // This needs to be static, so that we can share a single handler amongst all instances of BenchmarkClassicExecutor's
        private static ConsoleHandler consoleHandler;

        public ExecuteResult Execute(BuildResult buildResult, IDiagnoser diagnoser, Benchmark benchmark, ILogger logger)
        {
            if (consoleHandler == null)
            {
                consoleHandler = new ConsoleHandler(logger);
                Console.CancelKeyPress += consoleHandler.EventHandler;
            }

            var exeName = Path.Combine(buildResult.DirectoryPath, "Program.exe");
            var args = string.Empty;

            if (File.Exists(exeName))
            {
                try
                {
                    var startInfo = CreateStartInfo(benchmark, exeName, args);
                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            consoleHandler.SetProcess(process);
                            return ExecuteImpl(process, diagnoser, exeName, benchmark, logger);
                        }
                    }
                }
                finally
                {
                    consoleHandler?.ClearProcess();
                }
            }
            return new ExecuteResult(false, new string[0]);
        }

        private ExecuteResult ExecuteImpl(Process process, IDiagnoser diagnoser, string exeName, Benchmark benchmark, ILogger logger)
        {
            process.PriorityClass = ProcessPriorityClass.High;
            process.ProcessorAffinity = new IntPtr(2);

            var lines = new List<string>();
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                logger?.WriteLine(line);
                if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                    lines.Add(line);

                // Wait until we know "Warmup" is happening, and then dissassemble the process
                if (codeAlreadyExtracted == false && line.StartsWith("Warmup ") && !line.StartsWith("WarmupIdle "))
                {
                    try
                    {
                        diagnoser.Print(benchmark, process, logger);
                    }
                    finally
                    {
                        // Always set this, even if something went wrong, otherwise we will try on every run of a benchmark batch
                        codeAlreadyExtracted = true;
                    }
                }
            }

            if (process.HasExited && process.ExitCode != 0)
            {
                if (logger != null)
                {
                    logger.WriteError(
                        $"Something bad happened during the execution of {exeName}. Try to run the benchmark again using an AnyCPU application\n");
                }
                else
                {
                    if (exeName.ToLowerInvariant() == "msbuild")
                        Console.WriteLine("Build failed");
                }
                return new ExecuteResult(true, new string[0]);
            }

            return new ExecuteResult(true, lines);
        }

        private ProcessStartInfo CreateStartInfo(Benchmark benchmark, string exeName, string args)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
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