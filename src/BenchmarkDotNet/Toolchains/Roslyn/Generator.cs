using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    [PublicAPI]
    public class Generator : GeneratorBase
    {
        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
            => Path.GetDirectoryName(buildPartition.AssemblyLocation);

        [PublicAPI]
        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
            => new[]
            {
                artifactsPaths.ProgramCodePath,
                artifactsPaths.AppConfigPath,
                artifactsPaths.BuildScriptFilePath,
                artifactsPaths.ExecutablePath
            };

        protected override void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            string prefix = RuntimeInformation.IsWindows() ? "" : "#!/bin/bash\n";
            var list = new List<string>();
            if (!RuntimeInformation.IsWindows())
                list.Add("mono");
            list.Add("csc");
            list.Add("/noconfig");
            list.Add("/target:exe");
            list.Add("/optimize");
            list.Add("/unsafe");
            list.Add("/deterministic");
            list.Add("/platform:" + buildPartition.Platform.ToConfig());
            list.Add("/appconfig:" + artifactsPaths.AppConfigPath.EscapeCommandLine());
            var references = GetAllReferences(buildPartition.RepresentativeBenchmarkCase).Select(assembly => assembly.Location.EscapeCommandLine());
            list.Add("/reference:" + string.Join(",", references));
            list.Add(Path.GetFileName(artifactsPaths.ProgramCodePath));

            File.WriteAllText(
                artifactsPaths.BuildScriptFilePath,
                prefix + string.Join(" ", list));
        }

        internal static IEnumerable<Assembly> GetAllReferences(BenchmarkCase benchmarkCase)
            => benchmarkCase.Descriptor.Type.GetTypeInfo().Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(
                    new[]
                    {
                        benchmarkCase.Descriptor.Type.GetTypeInfo().Assembly, // this assembly does not has to have a reference to BenchmarkDotNet (e.g. custom framework for benchmarking that internally uses BenchmarkDotNet
                        typeof(BenchmarkCase).Assembly // BenchmarkDotNet
                    })
                .Distinct();
    }
}