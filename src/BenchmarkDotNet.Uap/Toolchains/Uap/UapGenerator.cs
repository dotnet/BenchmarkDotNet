#if !UAP
using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Uap.Helpers;
using System.IO;
using BenchmarkDotNet.Code;
using System.Reflection;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Uap
{
    internal class UapGenerator : GeneratorBase
    {
        private const string ProjectFileName = "UapBenchmarkProject.csproj";

        private readonly string uapBinariesFolder;
        private readonly Platform platform;

        public UapGenerator(string uapBinariesFolder, Platform platform)
        {
            this.uapBinariesFolder = uapBinariesFolder;
            this.platform = platform;
        }

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver, ILogger logger)
        {
            string content = ResourceHelperLocal.UapHelper.LoadTemplate("UapBenchmarkProject.notcsproj");

            var assemblyName = benchmark.Target.Type.GetTypeInfo().Assembly.GetName();
            content = SetGuid(content)
                .Replace("$BENCHMARKASSEMLYNAME$", assemblyName.Name)
                .Replace("$BENCHMARKASSEMLYPATH$", benchmark.Target.Type.GetTypeInfo().Assembly.Location)
                .Replace("$BDNCOREPATH$", Path.Combine(this.uapBinariesFolder, "BenchmarkDotNet.Runtime.dll"))
                .Replace("$BDNPATH$", Path.Combine(this.uapBinariesFolder, "BenchmarkDotNet.dll"));

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

        protected override void Cleanup(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BuildArtifactsDirectoryPath))
            {
                return;
            }

            int attempt = 0;
            while (attempt < 5)
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
                catch (Exception)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(500)); // Previous benchmark run didn't release some files
                    attempt++;
                }
            }

            if (attempt == 5)
            {
                Directory.Move(artifactsPaths.BuildArtifactsDirectoryPath, artifactsPaths.BuildArtifactsDirectoryPath + Guid.NewGuid());
            }
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string content = 
                             $"SET VS150COMNTOOLS=C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\Common7\\Tools\\{Environment.NewLine}" +
                             $"call \"%VS150COMNTOOLS%VsDevCmd.bat\"{Environment.NewLine}" +
                             $"msbuild /t:Restore{Environment.NewLine}" +
                             $"msbuild {ProjectFileName} /p:Configuration=Release;Platform={platform}";

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
            File.WriteAllText(Path.Combine(artifactsPaths.BinariesDirectoryPath, "App.xaml.cs"), CodeGenerator.Generate(benchmark, "UapBenchmarkProgram.txt", (name) => ResourceHelperLocal.UapHelper.LoadTemplate(name)));
        }

        protected override void CopyAllRequiredFiles(Benchmark benchmark, ArtifactsPaths artifactsPaths)
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
                File.WriteAllBytes(Path.Combine(assetsFolder, asset), ResourceHelperLocal.UapHelper.LoadBinaryFile(asset));
            }

            var xaml = "BDN.Generated.xaml";
            File.WriteAllText(Path.Combine(artifactsPaths.BinariesDirectoryPath, "App.xaml"), ResourceHelperLocal.UapHelper.LoadTemplate(xaml));
            var pfx = "BenchmarkDotNet.Autogenerated_TemporaryKey.pfx";
            File.WriteAllBytes(Path.Combine(artifactsPaths.BinariesDirectoryPath, pfx), ResourceHelperLocal.UapHelper.LoadBinaryFile(pfx));

            var manifest = "Package.appxmanifest";
            File.WriteAllBytes(Path.Combine(artifactsPaths.BinariesDirectoryPath, manifest), ResourceHelperLocal.UapHelper.LoadBinaryFile(manifest));

            var json = "UapBenchmarkProject.json";
            File.WriteAllBytes(Path.Combine(artifactsPaths.BinariesDirectoryPath, "project.json"), ResourceHelperLocal.UapHelper.LoadBinaryFile(json));

            var rd = "Default.rd.xml";
            var propertiesFolder = Path.Combine(artifactsPaths.BinariesDirectoryPath, "Properties");
            Directory.CreateDirectory(propertiesFolder);
            File.WriteAllBytes(Path.Combine(propertiesFolder, rd), ResourceHelperLocal.UapHelper.LoadBinaryFile(rd));

            var mainPageXaml = "MainPage.xaml";
            File.WriteAllText(Path.Combine(artifactsPaths.BinariesDirectoryPath, mainPageXaml), ResourceHelperLocal.UapHelper.LoadTemplate(mainPageXaml));

            var mainPageCs = "MainPage.xaml.notcs";
            File.WriteAllText(Path.Combine(artifactsPaths.BinariesDirectoryPath, Path.ChangeExtension(mainPageCs, ".cs")), ResourceHelperLocal.UapHelper.LoadTemplate(mainPageCs));
        }
    }
}
#endif