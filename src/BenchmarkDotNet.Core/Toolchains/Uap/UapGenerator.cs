using System;
using System.Linq;
#if UAP
using System.Threading.Tasks;
#else
using System.Threading;
#endif
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Helpers;
using System.IO;

namespace BenchmarkDotNet.Toolchains.Uap
{
    internal class UapGenerator : GeneratorBase
    {
        private const string ProjectFileName = "UapBenchmarkProject.csproj";

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string content = ResourceHelper.LoadTemplate("UapBenchmarkProject.notcsproj");
            content = SetGuid(content);

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);
        }

        private static string SetGuid(string template) => template.Replace("$GUID$", Guid.NewGuid().ToString());

        protected override string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName)
        {
            var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directoryInfo != null)
            {
                if (IsRootSolutionFolder(directoryInfo))
                {
                    return Path.Combine(directoryInfo.FullName, programName);
                }

                directoryInfo = directoryInfo.Parent;
            }

            // we did not find global.json or any Visual Studio solution file? 
            // let's return it in the old way and hope that it works ;)
            return Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName, programName);
        }

        protected override void Cleanup(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BuildArtifactsDirectoryPath))
            {
                return;
            }

            int attempt = 0;
            while (true)
            {
                try
                {
                    Directory.Delete(artifactsPaths.BuildArtifactsDirectoryPath, recursive: true);
                    return;
                }
                catch (DirectoryNotFoundException) // it's crazy but it happens ;)
                {
                    return;
                }
                catch (Exception) when (attempt++ < 5)
                {
#if UAP
                    Task.Delay(500).Wait();
#else
                    Thread.Sleep(TimeSpan.FromMilliseconds(500)); // Previous benchmark run didn't release some files
#endif
                }
            }
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string content = $"msbuild {ProjectFileName}";

            File.WriteAllText(artifactsPaths.BuildScriptFilePath, content);
        }

        private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                return false;
            }

            return directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => fileInfo.Extension == "sln" || fileInfo.Name == "global.json");
        }

        protected override string GetProjectFilePath(string binariesDirectoryPath)
        {
            return Path.Combine(binariesDirectoryPath, ProjectFileName);
        }

        protected override void GenerateCode(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            File.WriteAllText(artifactsPaths.ProgramCodePath, CodeGenerator.Generate(benchmark));
        }
    }
}
