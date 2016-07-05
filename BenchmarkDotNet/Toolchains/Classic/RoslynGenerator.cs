using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
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
            => Path.GetDirectoryName(benchmark.Target.Type.GetTypeInfo().Assembly.Location);

        protected override void Cleanup(ArtifactsPaths artifactsPaths)
        {
            DelteIfExists(artifactsPaths.ProgramCodePath);
            DelteIfExists(artifactsPaths.AppConfigPath);
            DelteIfExists(artifactsPaths.BuildScriptFilePath);
            DelteIfExists(artifactsPaths.ExecutablePath);
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            var prefix = RuntimeInformation.IsWindows() ? "" : "#!/bin/bash\n";
            var list = new List<string>();
            if (!RuntimeInformation.IsWindows())
                list.Add("mono");
            list.Add("csc");
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

        internal static IEnumerable<Assembly> GetAllReferences(Benchmark benchmark, bool includePredefined = false)
        {
            return (from referencedAssembly in benchmark.Target.Type.GetTypeInfo().Assembly.GetReferencedAssemblies()
                    where (includePredefined || !PredefinedAssemblies.Contains(referencedAssembly.Name))
                    select Assembly.Load(referencedAssembly))
                .Concat(new[] { benchmark.Target.Type.GetTypeInfo().Assembly });
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