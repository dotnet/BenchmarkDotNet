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
using BenchmarkDotNet.Code;
using System.Reflection;
using System.Diagnostics;

namespace BenchmarkDotNet.Toolchains.Uap
{
#if !UAP
    internal class UapGenerator : GeneratorBase
    {
        private const string ProjectFileName = "UapBenchmarkProject.csproj";

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string content = ResourceHelper.LoadTemplate("UapBenchmarkProject.notcsproj");

            var assemblyName = benchmark.Target.Type.GetTypeInfo().Assembly.GetName();
            content = SetGuid(content)
                .Replace("$BENCHMARKASSEMLYNAME$", assemblyName.Name)
                .Replace("$BENCHMARKASSEMLYPATH$", benchmark.Target.Type.GetTypeInfo().Assembly.Location)
                .Replace("$BDNCOREPATH$", benchmark.GetType().GetTypeInfo().Assembly.Location);

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
            string content = $"dotnet restore{Environment.NewLine}" +
                             $"call \"%VS140COMNTOOLS%VsDevCmd.bat\"{Environment.NewLine}" +
                             $"msbuild {ProjectFileName}";

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
            File.WriteAllText(Path.Combine(artifactsPaths.BinariesDirectoryPath, "App.xaml.cs"), CodeGenerator.Generate(benchmark, "UapBenchmarkProgram.txt"));
        }

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BinariesDirectoryPath))
            {
                Directory.CreateDirectory(artifactsPaths.BinariesDirectoryPath);
            }

            var assetsFolder = Path.Combine(artifactsPaths.BinariesDirectoryPath, "Assets");
            Directory.CreateDirectory(assetsFolder);

            string[] assetsFiles = { "Square150x150Logo.scale-200.png", "Square44x44Logo.scale-200.png", "StoreLogo.png" };

            foreach (var asset in assetsFiles)
            {
                File.WriteAllBytes(Path.Combine(assetsFolder, asset), ResourceHelper.LoadBinaryFile(asset));
            }

            var xaml = "BDN.Generated.xaml";
            File.WriteAllText(Path.Combine(artifactsPaths.BinariesDirectoryPath, "App.xaml"), ResourceHelper.LoadTemplate(xaml));
            var pfx = "BenchmarkDotNet.Autogenerated_TemporaryKey.pfx";
            File.WriteAllBytes(Path.Combine(artifactsPaths.BinariesDirectoryPath, pfx), ResourceHelper.LoadBinaryFile(pfx));

            var manifest = "Package.appxmanifest";
            File.WriteAllBytes(Path.Combine(artifactsPaths.BinariesDirectoryPath, manifest), ResourceHelper.LoadBinaryFile(manifest));

            var json = "UapBenchmarkProject.json";
            File.WriteAllBytes(Path.Combine(artifactsPaths.BinariesDirectoryPath, "project.json"), ResourceHelper.LoadBinaryFile(json));
        }
    }
#endif
}
