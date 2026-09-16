using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.Results;
using System.Diagnostics;

namespace BenchmarkDotNet.Toolchains.CoreRun
{
    public class CoreRunBuilder : CsProjBuilder
    {
        public CoreRunBuilder(FileInfo sourceCoreRun, FileInfo copyCoreRun, CoreRunSettings settings)
            : base(settings)
        {
            SourceCoreRun = sourceCoreRun;
            CopyCoreRun = copyCoreRun;
        }

        private FileInfo SourceCoreRun { get; }

        private FileInfo CopyCoreRun { get; }

        private bool NeedsCopy => SourceCoreRun != CopyCoreRun;

        protected override bool PublishesOutput => true;

        protected override async ValueTask<BuildResult> BuildAsync(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        {
            var buildResult = await base.BuildAsync(artifactsPaths, buildPartition, logger, cancellationToken).ConfigureAwait(false);

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
            var coreRunDirectory = CopyCoreRun.Directory!;

            foreach (var publishedDependency in publishedDirectory
                .EnumerateFileSystemInfos()
                .Where(file => file.Extension == ".dll" || file.Extension == ".exe"))
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

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, Settings.TargetFrameworkMoniker, "publish");

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (NeedsCopy)
                CopyFilesRecursively(SourceCoreRun.Directory!, CopyCoreRun.Directory!);

            base.CopyAllRequiredFiles(artifactsPaths);
        }

        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
            => NeedsCopy
                ? base.GetArtifactsToCleanup(artifactsPaths).Concat([CopyCoreRun.Directory!.FullName]).ToArray()
                : base.GetArtifactsToCleanup(artifactsPaths);

        // source: https://stackoverflow.com/a/58779/5852046
        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            if (!target.Exists)
                target.Create();

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), overwrite: true);
        }
    }
}