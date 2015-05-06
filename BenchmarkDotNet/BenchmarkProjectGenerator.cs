using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    internal class BenchmarkProjectGenerator
    {
        private const string MainClassName = "Program";

        public string GenerateProject(Benchmark benchmark)
        {
            var projectDir = CreateProjectDirectory(benchmark);
            GenerateProgramFile(projectDir, benchmark);
            GenerateProjectFile(projectDir, benchmark);
            GenerateAppConfigFile(projectDir, benchmark.Task.Configuration);
            return projectDir;
        }

        public void CompileCode(string directoryPath)
        {
            var executor = new BenchmarkExecutor();
            executor.Exec("MSBuild", Path.Combine(directoryPath, MainClassName + ".csproj"));
            Console.WriteLine(File.Exists(Path.Combine(directoryPath, MainClassName + ".exe")) ? "Success" : "Fail");
        }

        private static void GenerateProgramFile(string projectDir, Benchmark benchmark)
        {
            var targetType = benchmark.Target.Type.FullName;
            var targetTypeNamespace = benchmark.Target.Type.Namespace;
            var targetMethod = benchmark.Target.Method.Name;
            var targetMethodReturnType = benchmark.Target.Method.ReturnType.FullName;

            string runBenchmarkTemplate = "";
            switch (benchmark.Task.Configuration.Mode)
            {
                case BenchmarkMode.SingleRun:
                    {
                        runBenchmarkTemplate = GetTemplate("BenchmarkSingleRun.txt");
                    }
                    break;
            }
            var runBenchmarkContent = runBenchmarkTemplate.
                Replace("$TargetMethod$", targetMethod).
                Replace("$TargetMethodReturnType$", targetMethodReturnType);

            var contentTemplate = GetTemplate("BenchmarkProgram.txt");
            var content = contentTemplate.
                Replace("$TargetType$", targetType).
                Replace("$TargetTypeNamespace$", targetTypeNamespace).
                Replace("$RunBenchmarkContent$", runBenchmarkContent);

            string fileName = Path.Combine(projectDir, MainClassName + ".cs");
            File.WriteAllText(fileName, content);
        }

        private static void GenerateProjectFile(string projectDir, Benchmark benchmark)
        {
            var configuration = benchmark.Task.Configuration;
            var platform = configuration.Platform.ToConfig();
            var framework = configuration.Framework.ToConfig();
            var targetAssembly = new FileInfo(benchmark.Target.Type.Assembly.Location).Name;

            var template = GetTemplate("BenchmarkCsproj.txt");
            var content = template.
                Replace("$Platform$", platform).
                Replace("$Framework$", framework).
                Replace("$TargetAssembly$", targetAssembly);

            string fileName = Path.Combine(projectDir, MainClassName + ".csproj");
            File.WriteAllText(fileName, content);
        }

        private static void GenerateAppConfigFile(string projectDir, BenchmarkConfiguration configuration)
        {
            var useLagacyJit = configuration.JitVersion.ToConfig();

            var template = GetTemplate(configuration.JitVersion == BenchmarkJitVersion.CurrentJit ? "BenchmarkAppConfigEmpty.txt" : "BenchmarkAppConfig.txt");
            var content = template.
                Replace("$UseLagacyJit$", useLagacyJit);

            string fileName = Path.Combine(projectDir, "app.config");
            File.WriteAllText(fileName, content);
        }

        private static string CreateProjectDirectory(Benchmark benchmark)
        {
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), benchmark.Caption);
            try
            {
                if (Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, true);
            }
            catch (Exception)
            {
                // Nevermind
            }
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        private static string GetTemplate(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "BenchmarkDotNet.Templates." + name;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}