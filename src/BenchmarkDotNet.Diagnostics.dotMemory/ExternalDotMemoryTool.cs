using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;
using ILogger = BenchmarkDotNet.Loggers.ILogger;

namespace BenchmarkDotNet.Diagnostics.dotMemory
{
    internal class ExternalDotMemoryTool : DotMemoryToolBase
    {
        private static readonly TimeSpan AttachTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan SnapshotTimeout = TimeSpan.FromSeconds(30);

        private Process? process;
        private TaskCompletionSource<bool>? snapshotWaitingTask;

        public ExternalDotMemoryTool(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null) :
            base(logger, nugetUrl, nugetApi, downloadTo) { }

        protected override void Attach(DiagnoserActionParameters parameters, string snapshotFile)
        {
            var logger = parameters.Config.GetCompositeLogger();

            string runnerPath = GetRunnerPath();
            int pid = parameters.Process.Id;
            string arguments = $"attach {pid} --save-to-file=\"{snapshotFile}\" --service-output";

            logger.WriteLineInfo($"Starting process: '{runnerPath} {arguments}'");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = runnerPath,
                WorkingDirectory = "",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };

            var attachWaitingTask = new TaskCompletionSource<bool>();
            snapshotWaitingTask = new TaskCompletionSource<bool>();
            process = new Process { StartInfo = processStartInfo };
            try
            {
                process.OutputDataReceived += (_, args) =>
                {
                    string? content = args.Data;
                    if (content != null)
                    {
                        logger.WriteLineInfo("[dotMemory] " + content);
                        if (content.Contains("##dotMemory[\"connected\""))
                            attachWaitingTask.TrySetResult(true);

                        if (content.Contains("##dotMemory[\"snapshot-saved\""))
                            snapshotWaitingTask.TrySetResult(true);
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    string? content = args.Data;
                    if (content != null)
                        logger.WriteLineError("[dotMemory] " + args.Data);
                };
                process.Exited += (_, _) =>
                {
                    attachWaitingTask.TrySetResult(false);
                    snapshotWaitingTask.TrySetResult(false);
                };
                process.Start();
                process.StandardInput.AutoFlush = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                attachWaitingTask.TrySetResult(false);
                logger.WriteLineError(e.ToString());
            }

            if (!attachWaitingTask.Task.Wait(AttachTimeout))
                throw new Exception($"Failed to attach dotMemory to the target process (timeout: {AttachTimeout.TotalSeconds} sec)");
            if (!attachWaitingTask.Task.Result)
                throw new Exception($"Failed to attach dotMemory to the target process (ExitCode={process.ExitCode})");
        }

        protected override void Snapshot(DiagnoserActionParameters parameters)
        {
            if (process is null || snapshotWaitingTask is null)
                throw new InvalidOperationException("dotMemory process is not attached");

            if (snapshotWaitingTask.Task.IsCompleted)
            {
                if (!snapshotWaitingTask.Task.Result)
                    throw new InvalidOperationException($"Can't create dotMemory snapshot, dotMemory process exited (ExitCode={process.ExitCode})");

                snapshotWaitingTask = new TaskCompletionSource<bool>();
            }

            int pid = parameters.Process.Id;

            process.StandardInput.WriteLine($"##dotMemory[\"get-snapshot\", {{pid:{pid}}}]");

            if (!snapshotWaitingTask.Task.Wait(SnapshotTimeout))
                throw new Exception($"Failed to create dotMemory snapshot (timeout: {SnapshotTimeout.TotalSeconds} sec)");
            if (!snapshotWaitingTask.Task.Result)
                throw new Exception($"Failed to create dotMemory snapshot (ExitCode={process.ExitCode})");
        }

        protected override void Detach()
        {
            if (process is null)
                throw new InvalidOperationException("dotMemory process is not attached");

            process.StandardInput.WriteLine("##dotMemory[\"disconnect\"]");
            process.StandardInput.Close();
            process.WaitForExit();

            process.Dispose();
            process = null;
        }
    }
}