using System;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;
using BenchmarkDotNet.JetBrains;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    internal sealed class DotTraceTool
    {
        private readonly ILogger logger;
        private readonly Uri? nugetUrl;
        private readonly NuGetApi nugetApi;
        private readonly string? downloadTo;

        public DotTraceTool(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null)
        {
            this.logger = logger;
            this.nugetUrl = nugetUrl;
            this.nugetApi = nugetApi;
            this.downloadTo = downloadTo;
        }

        public void Init()
        {
            try
            {
                logger.WriteLineInfo("Ensuring that dotTrace prerequisite is installed...");
                var progress = new Progress(logger, "Installing DotTrace");
                DotTrace.EnsurePrerequisiteAsync(progress, nugetUrl, nugetApi, downloadTo).Wait();
                logger.WriteLineInfo("dotTrace prerequisite is installed");
                logger.WriteLineInfo($"dotTrace runner path: {Helper.GetRunnerPath(typeof(DotTrace))}");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }
        }

        public string Start(DiagnoserActionParameters parameters)
        {
            string snapshotFile = ArtifactFileNameHelper.GetFilePath(parameters, "snapshots", DateTime.Now, "dtp", ".0000".Length);
            string? snapshotDirectory = Path.GetDirectoryName(snapshotFile);
            logger.WriteLineInfo($"Target snapshot file: {snapshotFile}");
            if (!Directory.Exists(snapshotDirectory) && snapshotDirectory != null)
            {
                try
                {
                    Directory.CreateDirectory(snapshotDirectory);
                }
                catch (Exception e)
                {
                    logger.WriteLineError($"Failed to create directory: {snapshotDirectory}");
                    logger.WriteLineError(e.ToString());
                }
            }

            try
            {
                logger.WriteLineInfo("Attaching dotTrace to the process...");
                Attach(parameters, snapshotFile);
                logger.WriteLineInfo("dotTrace is successfully attached");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
                return snapshotFile;
            }

            try
            {
                logger.WriteLineInfo("Start collecting data using dotTrace...");
                StartCollectingData();
                logger.WriteLineInfo("Data collection has successfully started");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }

            return snapshotFile;
        }

        public void Stop()
        {
            try
            {
                logger.WriteLineInfo("Saving dotTrace snapshot...");
                SaveData();
                logger.WriteLineInfo("dotTrace snapshot is successfully saved to the artifact folder");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }

            try
            {
                logger.WriteLineInfo("Detaching dotTrace from the process...");
                Detach();
                logger.WriteLineInfo("dotTrace is successfully detached");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }
        }

        private void Attach(DiagnoserActionParameters parameters, string snapshotFile)
        {
            var config = new DotTrace.Config();

            var pid = parameters.Process.Id;
            var currentPid = Process.GetCurrentProcess().Id;
            if (pid != currentPid)
                config = config.ProfileExternalProcess(pid);

            config = config.SaveToFile(snapshotFile);
            DotTrace.Attach(config);
        }

        private void StartCollectingData() => DotTrace.StartCollectingData();

        private void SaveData() => DotTrace.SaveData();

        private void Detach() => DotTrace.Detach();
    }
}