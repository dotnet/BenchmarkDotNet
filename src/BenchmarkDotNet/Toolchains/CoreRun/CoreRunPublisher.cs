using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.CoreRun
{
    public class CoreRunPublisher : IBuilder
    {
        public CoreRunPublisher(FileInfo coreRun, FileInfo customDotNetCliPath = null)
        {
            CoreRun = coreRun;
            DotNetCliPublisher = new DotNetCliPublisher(customDotNetCliPath?.FullName);
        }

        private FileInfo CoreRun { get; }

        private DotNetCliPublisher DotNetCliPublisher { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var buildResult = DotNetCliPublisher.Build(generateResult, buildPartition, logger);

            if (buildResult.IsBuildSuccess)
                UpdateDuplicatedDependencies(buildResult.ArtifactsPaths, logger);

            return buildResult;
        }

        /// <summary>
        /// update CoreRun folder with newer versions of duplicated dependencies
        /// </summary>
        private void UpdateDuplicatedDependencies(ArtifactsPaths artifactsPaths, ILogger logger)
        {
            var publishedDirectory = new DirectoryInfo(artifactsPaths.BinariesDirectoryPath);
            var coreRunDirectory =  CoreRun.Directory;

            foreach (var publishedDependency in publishedDirectory
                .EnumerateFileSystemInfos()
                .Where(file => file.Extension == ".dll" || file.Extension == ".exe" ))
            {
                var coreRunDependency = new FileInfo(Path.Combine(coreRunDirectory.FullName, publishedDependency.Name));

                if (!coreRunDependency.Exists)
                    continue; // the file does not exist in CoreRun directory, we don't need to worry, it will be just loaded from publish directory by CoreRun

                var publishedVersionInfo = FileVersionInfo.GetVersionInfo(publishedDependency.FullName);
                var coreRunVersionInfo = FileVersionInfo.GetVersionInfo(coreRunDependency.FullName);

                if (!Version.TryParse(publishedVersionInfo.FileVersion, out var publishedVersion) || !Version.TryParse(coreRunVersionInfo.FileVersion, out var coreRunVersion))
                    continue;

                if (publishedVersion > coreRunVersion)
                {
                    File.Copy(publishedDependency.FullName, coreRunDependency.FullName, overwrite: true); // we need to overwrite old things with their newer versions

                    logger.WriteLineInfo($"Copying {publishedDependency.FullName} to {coreRunDependency.FullName}");
                }
            }
        }
    }
}