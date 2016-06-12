#if CLASSIC
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Configs;

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

        protected override string GetBinariesDirectoryPath(Benchmark benchmark, string rootArtifactsFolderPath, IConfig config)
        {
            if (config.KeepBenchmarkFiles)
            {
                return Path.Combine(rootArtifactsFolderPath, "bin", benchmark.ShortInfo);
            }

            return Path.Combine(rootArtifactsFolderPath, "bin", ShortFolderName);
        }

        protected override void CopyAllRequiredFiles(string rootArtifactsFolderPath, string binariesDirectoryPath, Benchmark benchmark)
        {
            CopyRoslynFiles(GetCompilerFolderPath(rootArtifactsFolderPath));

            CopyAllReferencedLibraries(binariesDirectoryPath, benchmark);
        }

        protected override void GenerateProjectBuildFile(string scriptFilePath, Benchmark benchmark, string rootArtifactsFolderPath, string appConfigPath)
        {
            var prefix = RuntimeInformation.IsWindows() ? "" : "#!/bin/bash\n";
            var list = new List<string>();
            if (!RuntimeInformation.IsWindows())
                list.Add("mono");
            list.Add(GetCompilerPath(rootArtifactsFolderPath).Escape());
            list.Add("/noconfig");
            list.Add("/target:exe");
            list.Add("/optimize");
            list.Add("/unsafe");
            list.Add("/platform:" + benchmark.Job.Platform.ToConfig());
            list.Add("/appconfig:" + appConfigPath.Escape());
            var refernces = GetAllReferences(benchmark).Select(a => a.Location.Escape());
            list.Add("/reference:" + string.Join(",", refernces));
            list.Add(ProgramFileName);

            File.WriteAllText(
                scriptFilePath, 
                prefix + string.Join(" ", list));
        }

        private string GetCompilerPath(string rootArtifactsFolderPath) => Path.Combine(GetCompilerFolderPath(rootArtifactsFolderPath), "csc.exe");

        private string GetCompilerFolderPath(string rootArtifactsFolderPath) => Path.Combine(rootArtifactsFolderPath, "Roslyn");

        private void CopyRoslynFiles(string compilerFolderPath)
        {
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

        private void CopyAllReferencedLibraries(string binariesDirectoryPath, Benchmark benchmark)
        {
            foreach (var assembly in GetAllReferences(benchmark).Where(assemlby => !assemlby.GlobalAssemblyCache))
            {
                File.Copy(assembly.Location, Path.Combine(binariesDirectoryPath, $"{assembly.GetName().Name}.dll"));
            }
        }

        private static IEnumerable<Assembly> GetAllReferences(Benchmark benchmark)
        {
            var uniqueDependencies = new HashSet<Assembly>();
            var assembliesToCheck = new Stack<AssemblyName>(benchmark.Target.Type.Assembly.GetReferencedAssemblies());
            assembliesToCheck.Push(benchmark.Target.Type.Assembly.GetName());

            while (assembliesToCheck.Any())
            {
                var assemblyName = assembliesToCheck.Pop();
                if (PredefinedAssemblies.Contains(assemblyName.Name))
                {
                    continue;
                }

                var loaded = Assembly.Load(assemblyName);
                if (uniqueDependencies.Contains(loaded))
                {
                    continue;
                }

                uniqueDependencies.Add(loaded);
                if (loaded.GlobalAssemblyCache)
                {
                    continue;
                }

                foreach (var referencedAssembly in loaded.GetReferencedAssemblies())
                {
                    assembliesToCheck.Push(referencedAssembly);
                }
            }

            return uniqueDependencies;
        }
    }
}
#endif