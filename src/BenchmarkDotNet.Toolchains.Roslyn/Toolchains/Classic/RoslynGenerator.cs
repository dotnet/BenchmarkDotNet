using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class RoslynGenerator : GeneratorBase
    {
        protected override string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName)
            => Path.GetDirectoryName(benchmark.Target.Type.GetTypeInfo().Assembly.Location);

        protected override void Cleanup(ArtifactsPaths artifactsPaths)
        {
            DelteIfExists(artifactsPaths.ProgramCodePath);
            DelteIfExists(artifactsPaths.AppConfigPath);
            DelteIfExists(artifactsPaths.BuildScriptFilePath);
            DelteIfExists(artifactsPaths.ExecutablePath);
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
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
            list.Add("/platform:" + benchmark.Job.Env.Platform.Resolve(resolver).ToConfig());
            list.Add("/appconfig:" + artifactsPaths.AppConfigPath.Escape());
            var references = GetAllReferences(benchmark).Select(assembly => assembly.Location.Escape());
            list.Add("/reference:" + string.Join(",", references));
            list.Add(Path.GetFileName(artifactsPaths.ProgramCodePath));

            File.WriteAllText(
                artifactsPaths.BuildScriptFilePath,
                prefix + string.Join(" ", list));
        }

        internal static IEnumerable<Assembly> GetAllReferences(Benchmark benchmark)
        {
            return benchmark.Target.Type.GetTypeInfo().Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
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