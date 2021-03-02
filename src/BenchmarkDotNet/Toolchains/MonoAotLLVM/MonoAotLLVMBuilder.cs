using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.MonoAotLLVM
{
    public class MonoAotLLVMBuilder : IBuilder
    {
        private readonly DotNetCliBuilder dotnetCliBuilder;

        public MonoAotLLVMBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null, TimeSpan? timeout = null)
        {
            dotnetCliBuilder = new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath, timeout);
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = dotnetCliBuilder.Build(generateResult, buildPartition, logger);

            RenameSharedLibaries(generateResult, logger);

            return buildResult;
        }

        // This method is as work around because the MonoAOTCompilerTask
        // does not generate files with the right names.
        // In the future it may have an option to write .so/.dylib files,
        // and we can get rid of this method.
        public void RenameSharedLibaries(GenerateResult generateResult, ILogger logger)
        {
            string[] publishDirFiles = Directory.GetFiles(generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath, ".dll.o");
            string sharedObjectExtension = "";

            if (RuntimeInformation.IsMacOSX())
            {
                sharedObjectExtension = "dylib";
            }
            else if (RuntimeInformation.IsLinux())
            {
                sharedObjectExtension = "so";
            }

            foreach (string fileName in publishDirFiles)
            {
                string newFileName = Path.ChangeExtension(fileName, sharedObjectExtension);

                logger.WriteLine($"RenameSharedLibraries: Moving ${fileName} to {newFileName}");
                System.IO.File.Move(fileName, newFileName);
            }
        }
    }
}
