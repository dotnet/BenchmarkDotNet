using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;

namespace BenchmarkDotNet.Diagnostics.dotMemory
{
    internal sealed class DotMemoryTool
    {
        private readonly ILogger logger;
        private readonly Uri? nugetUrl;
        private readonly NuGetApi nugetApi;
        private readonly string? downloadTo;

        public DotMemoryTool(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null)
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
                logger.WriteLineInfo("Ensuring that dotMemory prerequisite is installed...");
                var progress = new Progress(logger, "Installing DotMemory");
                DotMemory.EnsurePrerequisiteAsync(progress, nugetUrl, nugetApi, downloadTo).Wait();
                logger.WriteLineInfo("dotMemory prerequisite is installed");
                logger.WriteLineInfo($"dotMemory runner path: {GetRunnerPath()}");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }
        }

        public string Start(DiagnoserActionParameters parameters)
        {
            string snapshotFile = ArtifactFileNameHelper.GetFilePath(parameters, "snapshots", DateTime.Now, "dmw", ".0000".Length);
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
                logger.WriteLineInfo("Attaching dotMemory to the process...");
                Attach(parameters, snapshotFile);
                logger.WriteLineInfo("dotMemory is successfully attached");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
                return snapshotFile;
            }

            return snapshotFile;
        }

        public void Stop()
        {
            try
            {
                logger.WriteLineInfo("Taking dotMemory snapshot...");
                Snapshot();
                logger.WriteLineInfo("dotMemory snapshot is successfully taken");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }

            try
            {
                logger.WriteLineInfo("Detaching dotMemory from the process...");
                Detach();
                logger.WriteLineInfo("dotMemory is successfully detached");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }
        }

        private void Attach(DiagnoserActionParameters parameters, string snapshotFile)
        {
            var config = new DotMemory.Config();

            var pid = parameters.Process.Id;
            var currentPid = Process.GetCurrentProcess().Id;
            if (pid != currentPid)
                config = config.ProfileExternalProcess(pid);

            config = config.SaveToFile(snapshotFile);
            DotMemory.Attach(config);
        }

        private void Snapshot() => DotMemory.GetSnapshot();

        private void Detach() => DotMemory.Detach();

        private string GetRunnerPath()
        {
            var consoleRunnerPackageField = typeof(DotMemory).GetField("ConsoleRunnerPackage", BindingFlags.NonPublic | BindingFlags.Static);
            if (consoleRunnerPackageField == null)
                throw new InvalidOperationException("Field 'ConsoleRunnerPackage' not found.");

            object? consoleRunnerPackage = consoleRunnerPackageField.GetValue(null);
            if (consoleRunnerPackage == null)
                throw new InvalidOperationException("Unable to get value of 'ConsoleRunnerPackage'.");

            var consoleRunnerPackageType = consoleRunnerPackage.GetType();
            var getRunnerPathMethod = consoleRunnerPackageType.GetMethod("GetRunnerPath");
            if (getRunnerPathMethod == null)
                throw new InvalidOperationException("Method 'GetRunnerPath' not found.");

            string? runnerPath = getRunnerPathMethod.Invoke(consoleRunnerPackage, null) as string;
            if (runnerPath == null)
                throw new InvalidOperationException("Unable to invoke 'GetRunnerPath'.");

            return runnerPath;
        }
    }
}