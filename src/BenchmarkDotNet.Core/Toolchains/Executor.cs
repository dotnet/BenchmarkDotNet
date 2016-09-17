using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    internal class Executor : IExecutor
    {
        // This needs to be static, so that we can share a single handler amongst all instances of Executor's
        private static ConsoleHandler consoleHandler;

        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser compositeDiagnoser = null)
        {
            var exePath = buildResult.ArtifactsPaths.ExecutablePath;
            var args = string.Empty;

            if (!File.Exists(exePath))
            {
                return new ExecuteResult(false, -1, new string[0]);
            }

            return Execute(benchmark, logger, exePath, null, args, compositeDiagnoser, resolver);
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, string exeName, string workingDirectory, string args, IDiagnoser diagnoser,
            IResolver resolver)
        {
            if (consoleHandler == null)
            {
                consoleHandler = new ConsoleHandler(logger);
                Console.CancelKeyPress += consoleHandler.EventHandler;
            }

            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(benchmark, exeName, args, workingDirectory, resolver) })
                {
                    var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark);

                    return Execute(process, benchmark, loggerWithDiagnoser, diagnoser, logger);
                }
            }
            finally
            {
                consoleHandler.ClearProcess();
            }
        }

        private ExecuteResult Execute(Process process, Benchmark benchmark, SynchronousProcessOutputLoggerWithDiagnoser loggerWithDiagnoser,
            IDiagnoser compositeDiagnoser, ILogger logger)
        {
            consoleHandler.SetProcess(process);

            process.Start();

            compositeDiagnoser?.ProcessStarted(process);

            process.EnsureHighPriority(logger);
            if (!benchmark.Job.Env.Affinity.IsDefault)
            {
                process.EnsureProcessorAffinity(benchmark.Job.Env.Affinity.SpecifiedValue);
            }

            loggerWithDiagnoser.ProcessInput();

            process.WaitForExit(); // should we add timeout here?

            compositeDiagnoser?.ProcessStopped(process);

            if (process.ExitCode == 0)
            {
                return new ExecuteResult(true, process.ExitCode, loggerWithDiagnoser.Lines);
            }

            return new ExecuteResult(true, process.ExitCode, new string[0]);
        }

        private ProcessStartInfo CreateStartInfo(Benchmark benchmark, string exeName, string args, string workingDirectory, IResolver resolver)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            var runtime = benchmark.Job.Env.Runtime.IsDefault ? RuntimeInformation.GetCurrentRuntime() : benchmark.Job.Env.Runtime.SpecifiedValue;
                // TODO: use resolver
            switch (runtime)
            {
                case Runtime.Clr:
                case Runtime.Core:
                    start.FileName = exeName;
                    start.Arguments = args;
                    break;
                case Runtime.Mono:
                    start.FileName = "mono";
                    start.Arguments = GetMonoArguments(benchmark.Job, exeName, args, resolver);
                    break;
                default:
                    throw new NotSupportedException("Runtime = " + runtime);
            }
            return start;
        }

        private string GetMonoArguments(Job job, string exeName, string args, IResolver resolver)
        {
            // from mono --help: "Usage is: mono [options] program [program-options]"
            return new StringBuilder(30)
                .Append(job.Env.Jit.Resolve(resolver) == Jit.Llvm ? "--llvm" : "--nollvm")
                .Append(' ')
                .Append(exeName)
                .Append(' ')
                .Append(args)
                .ToString();
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