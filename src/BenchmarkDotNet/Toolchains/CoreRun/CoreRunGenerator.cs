using System.IO;
using System.Linq;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains.CoreRun
{
    public class CoreRunGenerator : CsProjGenerator
    {
        public CoreRunGenerator(FileInfo sourceCoreRun, FileInfo copyCoreRun, string targetFrameworkMoniker, string cliPath, string packagesPath)
            : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion: null)
        {
            SourceCoreRun = sourceCoreRun;
            CopyCoreRun = copyCoreRun;
        }

        private FileInfo SourceCoreRun { get; }

        private FileInfo CopyCoreRun { get; }

        private bool NeedsCopy => SourceCoreRun != CopyCoreRun;

        protected override string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) => PackagesPath;

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, "publish");

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (NeedsCopy)
                CopyFilesRecursively(SourceCoreRun.Directory, CopyCoreRun.Directory);

            base.CopyAllRequiredFiles(artifactsPaths);
        }

        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
            => NeedsCopy
                ? base.GetArtifactsToCleanup(artifactsPaths).Concat(new[] { CopyCoreRun.Directory.FullName }).ToArray()
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