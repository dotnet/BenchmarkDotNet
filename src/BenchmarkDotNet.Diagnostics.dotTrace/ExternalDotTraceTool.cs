using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;
using ILogger = BenchmarkDotNet.Loggers.ILogger;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    internal class ExternalDotTraceTool : DotTraceToolBase
    {
        private static readonly TimeSpan AttachTimeout = TimeSpan.FromMinutes(5);

        public ExternalDotTraceTool(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null) :
            base(logger, nugetUrl, nugetApi, downloadTo) { }

        protected override bool AttachOnly => true;

        protected override void Attach(DiagnoserActionParameters parameters, string snapshotFile)
        {
            var logger = parameters.Config.GetCompositeLogger();

            string runnerPath = GetRunnerPath();
            int pid = parameters.Process.Id;
            string arguments = $"attach {pid} --save-to=\"{snapshotFile}\" --service-output=on";

            logger.WriteLineInfo($"Starting process: '{runnerPath} {arguments}'");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = runnerPath,
                WorkingDirectory = "",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var attachWaitingTask = new TaskCompletionSource<bool>();
            var process = new Process { StartInfo = processStartInfo };
            try
            {
                process.OutputDataReceived += (_, args) =>
                {
                    string? content = args.Data;
                    if (content != null)
                    {
                        logger.WriteLineInfo("[dotTrace] " + content);
                        if (content.Contains("##dotTrace[\"started\""))
                            attachWaitingTask.TrySetResult(true);
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    string? content = args.Data;
                    if (content != null)
                        logger.WriteLineError("[dotTrace] " + args.Data);
                };
                process.Exited += (_, _) => { attachWaitingTask.TrySetResult(false); };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                attachWaitingTask.TrySetResult(false);
                logger.WriteLineError(e.ToString());
            }

            if (!attachWaitingTask.Task.Wait(AttachTimeout))
                throw new Exception($"Failed to attach dotTrace to the target process (timeout: {AttachTimeout.TotalSeconds} sec");
            if (!attachWaitingTask.Task.Result)
                throw new Exception($"Failed to attach dotTrace to the target process (ExitCode={process.ExitCode})");
        }

        protected override void StartCollectingData() { }

        protected override void SaveData() { }

        protected override void Detach() { }
    }
}