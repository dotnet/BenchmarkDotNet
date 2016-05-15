#if CLASSIC
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class RoslynGenerator : GeneratorBase
    {
        protected override string GetProgramName(Benchmark benchmark) => benchmark.ShortInfo;

        private static readonly HashSet<string> PredefinedAssemblies = new HashSet<string>(
            new[]
            {
                "mscorlib",
                "System",
                "System.Core",
                "System.Xml.Linq",
                "System.Xml"
            });

        public override GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath)
        {
            var directory = Directory.GetCurrentDirectory();

            GenerateBuildScript(directory, benchmark);
            GenerateProgramFile(directory, benchmark);

            return new GenerateResult(directory, benchmark.ShortInfo, true, null);
        }
      
        protected void GenerateBuildScript(string projectDir, Benchmark benchmark)
        {
            var fileName = Path.Combine(projectDir, benchmark.ShortInfo + "-" + BuildBenchmarkScriptFileName);
            File.WriteAllText(fileName, CreateRoslynCompileCommand(benchmark, benchmark.ShortInfo));
        }

        private static IEnumerable<Assembly> GetAllReferences(Benchmark benchmark)
        {
            return benchmark.Target.Type.Assembly
                .GetReferencedAssemblies()
                .Concat(new[] { benchmark.Target.Type.Assembly.GetName() })
                .Where(assemblyName => !PredefinedAssemblies.Contains(assemblyName.Name))
                .Select(Assembly.Load)
                .Where(assembly => !assembly.GlobalAssemblyCache);
        }

        #region Roslyn

        private const string RoslynResourcePrefix = "BenchmarkDotNet.Roslyn.";

        public static string GetDefaultDirectory() => Path.Combine(Directory.GetCurrentDirectory(), "Roslyn");

        private static void CreateIfNotExist()
        {
            var folder = GetDefaultDirectory();
            if (!Directory.Exists(folder))
                Create(GetDefaultDirectory());
        }

        public static void Create(string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            foreach (var resource in ResourceHelper.GetAllResources(RoslynResourcePrefix))
            {
                var fileName = resource.Substring(RoslynResourcePrefix.Length);
                using (var input = ResourceHelper.GetResouceStream(resource))
                using (var output = File.Create(Path.Combine(destFolder, fileName)))
                    StreamHelper.CopyStream(input, output);
            }
        }

        public static string GetCscExePath()
        {
            CreateIfNotExist();
            return Path.Combine(GetDefaultDirectory(), "csc.exe");
        }

        public static string CreateRoslynCompileCommand(Benchmark benchmark, string programName)
        {
            var prefix = RuntimeInformation.IsWindows() ? "" : "#!/bin/bash\n";
            var list = new List<string>();
            if (!RuntimeInformation.IsWindows())
                list.Add("mono");
            list.Add(Escape(GetCscExePath()));
            list.Add("/noconfig");
            list.Add("/target:exe");
            list.Add("/optimize");
            list.Add("/unsafe");
            list.Add("/platform:" + benchmark.Job.Platform.ToConfig());
            // list.Add("/appconfig:" + AppConfigFileName);
            var refernces = GetAllReferences(benchmark).Select(a => Escape(a.Location));
            list.Add("/reference:" + string.Join(",", refernces));
            list.Add(programName + ".cs");
            return prefix + string.Join(" ", list);
        }

        private static string Escape(string path) => "\"" + path + "\"";

        #endregion

        protected override string GetBinariesDirectoryPath(Benchmark benchmark, string rootArtifactsFolderPath)
        {
            throw new System.NotImplementedException();
        }

        protected override void GenerateProjectFile(ILogger logger, string projectDir, Benchmark benchmark)
        {
            throw new System.NotImplementedException();
        }

        protected override void GenerateProjectBuildFile(string scriptFilePath, Framework framework)
        {
            throw new System.NotImplementedException();
        }
    }
}
#endif