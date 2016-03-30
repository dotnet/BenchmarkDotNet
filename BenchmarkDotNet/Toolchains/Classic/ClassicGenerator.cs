#if CLASSIC
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class ClassicGenerator : GeneratorBase
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

        protected override string GetBinariesDirectoryPath(Benchmark benchmark, string rootArtifactsFolderPath)
        {
            return Path.Combine(rootArtifactsFolderPath, "bin", benchmark.ShortInfo);
        }

        protected override void GenerateProjectFile(ILogger logger, string projectDir, Benchmark benchmark)
        {
            var job = benchmark.Job;
            var platform = job.Platform.ToConfig();
            var targetFrameworkVersion = GetTargetFrameworkVersion(benchmark);

            var template = ResourceHelper.LoadTemplate("BenchmarkCsproj.txt");
            var content = template
                .Replace("$Platform$", platform)
                .Replace("$Framework$", targetFrameworkVersion)
                .Replace("$CustomReferences$", BuildCustomReferences(benchmark));

            string fileName = Path.Combine(projectDir, MainClassName + ".csproj");
            File.WriteAllText(fileName, content);
        }

        protected override void GenerateProjectBuildFile(string scriptFilePath)
        {
            var content = ResourceHelper.LoadTemplate("BuildBenchmark.txt");
            File.WriteAllText(scriptFilePath, content);
        }

        private string GetTargetFrameworkVersion(Benchmark benchmark)
        {
            if (benchmark.Job.Framework != Framework.Host) 
            {
                // user decided to configure it in explicit way
                return benchmark.Job.Framework.ToConfig();
            }

            // let's take the highest value we can to ensure that we don't fall to any framework mismatch problems
            return GetAllKnownTargetFrameworkVersions(benchmark).Max();
        }

        private IEnumerable<string> GetAllKnownTargetFrameworkVersions(Benchmark benchmark)
        {
            yield return benchmark.Target.Type.Assembly().GetTargetFrameworkVersion(); // the dll that defines benchmark
            yield return Assembly.GetExecutingAssembly().GetTargetFrameworkVersion(); // the executing program

            foreach (var assemblyName in benchmark.Target.Type.Assembly.GetReferencedAssemblies())
            {
                yield return Assembly.Load(assemblyName).GetTargetFrameworkVersion();
            }
        }

        private string BuildCustomReferences(Benchmark benchmark)
        {
            var buffer = new StringBuilder();

            foreach (var assemblyName in benchmark.Target.Type.Assembly.GetReferencedAssemblies()
                .Concat(new[] { benchmark.Target.Type.Assembly.GetName() }) // do not forget about the dll that defines benchmark!
                .Where(assemblyName => !PredefinedAssemblies.Contains(assemblyName.Name)))
            {
                var referencedAssembly = Assembly.Load(assemblyName);

                buffer.Append($"<Reference Include=\"{assemblyName.FullName}\">")
                      .Append(GetHintPath(referencedAssembly))
                      .AppendLine("</Reference>");
            }

            return buffer.ToString();
        }

        private string GetHintPath(Assembly loadedReferencedAssembly)
        {
            if (loadedReferencedAssembly.GlobalAssemblyCache)
            {
                // there is no need to specify path if assembly was loaded from GAC
                return string.Empty;
            }

            // the assembly is loaded so we just give the absolute path
            return $"<HintPath>{loadedReferencedAssembly.Location}</HintPath>";
        }
    }
}
#endif