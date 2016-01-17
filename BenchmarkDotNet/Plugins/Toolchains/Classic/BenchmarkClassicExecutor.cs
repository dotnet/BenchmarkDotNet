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

        public BenchmarkExecResult Execute(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser)
        {
            var exeName = Path.Combine(buildResult.DirectoryPath, "Program.exe");
            var args = parameters == null ? string.Empty : parameters.ToArgs();

            if (File.Exists(exeName))
            {
                try
                {
                    var startInfo = CreateStartInfo(exeName, args);
                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            consoleHandler.SetProcess(process);
                            return ExecuteImpl(process, diagnoser, exeName);
                        }
                    }
                }
                finally
                {
                    consoleHandler?.ClearProcess();
                }
            }
            return new BenchmarkExecResult(false, new string[0]);
        }

        private BenchmarkExecResult ExecuteImpl(Process process, IBenchmarkDiagnoser diagnoser, string exeName)
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
                return new BenchmarkExecResult(true, new string[0]);
            }

            return new BenchmarkExecResult(true, lines);
        }

        private ProcessStartInfo CreateStartInfo(string exeName, string args)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
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

            public ConsoleHandler( IBenchmarkLogger logger)
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