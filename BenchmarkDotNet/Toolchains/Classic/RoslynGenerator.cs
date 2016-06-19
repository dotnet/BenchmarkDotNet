#if CLASSIC
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class RoslynGenerator : GeneratorBase
    {
        private static readonly HashSet<string> PredefinedAssemblies = new HashSet<string>(
            new[]
            {
                "mscorlib",
                "System",
                "System.Core",
                "System.Xml.Linq",
                "System.Xml"
            });

        protected override string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName)
            => Path.GetDirectoryName(benchmark.Target.Type.Assembly.Location);

        protected override string GetCompilerPath(string rootArtifactsFolderPath)
            => Path.Combine(GetCompilerFolderPath(rootArtifactsFolderPath), "csc.exe");

        protected override void Cleanup(ArtifactsPaths artifactsPaths)
        {
            DelteIfExists(artifactsPaths.ProgramCodePath);
            DelteIfExists(artifactsPaths.AppConfigPath);
            DelteIfExists(artifactsPaths.BuildScriptFilePath);
            DelteIfExists(artifactsPaths.ExecutablePath);
        }

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            string compilerFolderPath = GetCompilerFolderPath(artifactsPaths.RootArtifactsFolderPath);
            if (Directory.Exists(compilerFolderPath))
            {
                return;
            }

            if (!Directory.Exists(compilerFolderPath))
            {
                Directory.CreateDirectory(compilerFolderPath);
            }

            const string roslynResourcePrefix = "BenchmarkDotNet.Roslyn.";
            foreach (var resource in ResourceHelper.GetAllResources(roslynResourcePrefix))
            {
                var fileName = resource.Substring(roslynResourcePrefix.Length);
                using (var input = ResourceHelper.GetResouceStream(resource))
                using (var output = File.Create(Path.Combine(compilerFolderPath, fileName)))
                    StreamHelper.CopyStream(input, output);
            }
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            var prefix = RuntimeInformation.IsWindows() ? "" : "#!/bin/bash\n";
            var list = new List<string>();
            if (!RuntimeInformation.IsWindows())
                list.Add("mono");
            list.Add(artifactsPaths.CompilerPath.Escape());
            list.Add("/noconfig");
            list.Add("/target:exe");
            list.Add("/optimize");
            list.Add("/unsafe");
            list.Add("/platform:" + benchmark.Job.Platform.ToConfig());
            list.Add("/appconfig:" + artifactsPaths.AppConfigPath.Escape());
            var references = GetAllReferences(benchmark).Select(assembly => assembly.Location.Escape());
            list.Add("/reference:" + string.Join(",", references));
            list.Add(Path.GetFileName(artifactsPaths.ProgramCodePath));

            File.WriteAllText(
                artifactsPaths.BuildScriptFilePath,
                prefix + string.Join(" ", list));
        }

        private string GetCompilerFolderPath(string rootArtifactsFolderPath)
            => Path.Combine(rootArtifactsFolderPath, "Roslyn");

        private static IEnumerable<Assembly> GetAllReferences(Benchmark benchmark)
        {
            return (from referencedAssembly in benchmark.Target.Type.Assembly.GetReferencedAssemblies()
                    where !PredefinedAssemblies.Contains(referencedAssembly.Name)
                    select Assembly.Load(referencedAssembly))
                .Concat(new[] { benchmark.Target.Type.Assembly });
        }

        private static void DelteIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
#endif