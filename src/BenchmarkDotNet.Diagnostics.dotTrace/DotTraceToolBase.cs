using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    internal abstract class DotTraceToolBase
    {
        private readonly ILogger logger;
        private readonly Uri? nugetUrl;
        private readonly NuGetApi nugetApi;
        private readonly string? downloadTo;

        protected DotTraceToolBase(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null)
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
                logger.WriteLineInfo("Ensuring that dotTrace prerequisite is installed...");
                var progress = new Progress(logger, "Installing DotTrace");
                DotTrace.EnsurePrerequisiteAsync(progress, nugetUrl, nugetApi, downloadTo).Wait();
                logger.WriteLineInfo("dotTrace prerequisite is installed");
                logger.WriteLineInfo($"dotTrace runner path: {GetRunnerPath()}");
            }
            catch (Exception e)
            {
                logger.WriteLineError(e.ToString());
            }
        }

        protected abstract bool AttachOnly { get; }
        protected abstract void Attach(DiagnoserActionParameters parameters, string snapshotFile);
        protected abstract void StartCollectingData();
        protected abstract void SaveData();
        protected abstract void Detach();

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

            if (!AttachOnly)
            {
                try
                {
                    logger.WriteLineInfo("Start collecting data using dataTrace...");
                    StartCollectingData();
                    logger.WriteLineInfo("Data collecting is successfully started");
                }
                catch (Exception e)
                {
                    logger.WriteLineError(e.ToString());
                }
            }

            return snapshotFile;
        }

        public void Stop(DiagnoserActionParameters parameters)
        {
            if (!AttachOnly)
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
        }

        protected string GetRunnerPath()
        {
            var consoleRunnerPackageField = typeof(DotTrace).GetField("ConsoleRunnerPackage", BindingFlags.NonPublic | BindingFlags.Static);
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