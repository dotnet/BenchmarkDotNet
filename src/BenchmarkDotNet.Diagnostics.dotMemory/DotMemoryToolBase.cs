using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;

namespace BenchmarkDotNet.Diagnostics.dotMemory
{
    internal abstract class DotMemoryToolBase
    {
        private readonly ILogger logger;
        private readonly Uri? nugetUrl;
        private readonly NuGetApi nugetApi;
        private readonly string? downloadTo;

        protected DotMemoryToolBase(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null)
        {
            this.logger = logger;
            this.nugetUrl = nugetUrl;
            this.nugetApi = nugetApi;
            this.downloadTo = downloadTo;
        }

        public void Init(DiagnoserActionParameters parameters)
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

        protected abstract void Attach(DiagnoserActionParameters parameters, string snapshotFile);
        protected abstract void Snapshot(DiagnoserActionParameters parameters);
        protected abstract void Detach();

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

        public void Stop(DiagnoserActionParameters parameters)
        {
            try
            {
                logger.WriteLineInfo("Taking dotMemory snapshot...");
                Snapshot(parameters);
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

        protected string GetRunnerPath()
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