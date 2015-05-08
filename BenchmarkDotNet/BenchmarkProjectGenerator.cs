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
            var isVoid = benchmark.Target.Method.ReturnType == typeof(void);

            var targetType = benchmark.Target.Type.FullName;
            var targetTypeNamespace = benchmark.Target.Type.Namespace;
            var targetMethod = benchmark.Target.Method.Name;
            var targetMethodDelegate = "targetMethodDelegate";
            var targetMethodReturnType = benchmark.Target.Method.ReturnType == typeof(void)
                ? "void"
                : benchmark.Target.Method.ReturnType.GetCorrectTypeName();
            var targetMethodReturnTypeNamespace = benchmark.Target.Method.ReturnType == typeof(void)
                ? "System"
                : benchmark.Target.Method.ReturnType.Namespace;
            var operationsPerMethod = benchmark.Target.OperationsPerMethod;
            var targetMethodResultHolder = isVoid ? "" : $"private {targetMethodReturnType} value;";
            var targetMethodHoldValue = isVoid ? "" : "value = ";
            var dummyDelegate = "dummyDelegate";
            var targetMethodDelegateDeclaration =
                "private " +
                (isVoid ? "Action " : $"Func<{targetMethodReturnType}> ") +
                targetMethodDelegate +
                ";";
            var targetMethodDelegateDefinition = "() => instance." + targetMethod + "()";
            var dummyDelegateDeclaration =
                "private " +
                (isVoid ? "Action " : $"Func<{targetMethodReturnType}> ") +
                dummyDelegate +
                ";";
            var dummyDelegateDefinition = "() => instance.Dummy()";
            var dummyImplementation = isVoid
                ? ""
                : $"return default({targetMethodReturnType});";

            string runBenchmarkTemplate = "";
            switch (benchmark.Task.Configuration.Mode)
            {
                case BenchmarkMode.SingleRun:
                    runBenchmarkTemplate = GetTemplate("BenchmarkSingleRun.txt");
                    break;
                case BenchmarkMode.Throughput:
                    runBenchmarkTemplate = GetTemplate("BenchmarkThroughput.txt");
                    break;
            }

            var contentTemplate = GetTemplate("BenchmarkProgram.txt");
            var content = contentTemplate.
                Replace("$RunBenchmarkContent$", runBenchmarkTemplate).
                Replace("$TargetMethod$", targetMethodDelegate).
                Replace("$TargetMethodReturnType$", targetMethodReturnType).
                Replace("$OperationsPerMethod$", operationsPerMethod.ToInvariantString()).
                Replace("$TargetMethodResultHolder$", targetMethodResultHolder).
                Replace("$TargetMethodHoldValue$", targetMethodHoldValue).
                Replace("$TargetType$", targetType).
                Replace("$TargetTypeNamespace$", targetTypeNamespace).
                Replace("$TargetMethodReturnTypeNamespace$", targetMethodReturnTypeNamespace).
                Replace("$TargetMethodDelegateDeclaration$", targetMethodDelegateDeclaration).
                Replace("$TargetMethodDelegate$", targetMethodDelegate).
                Replace("$TargetMethodDelegateDefinition$", targetMethodDelegateDefinition).
                Replace("$DummyDelegateDeclaration$", dummyDelegateDeclaration).
                Replace("$DummyDelegateDefinition$", dummyDelegateDefinition).
                Replace("$DummyDelegate$", dummyDelegate).
                Replace("$DummyImplementation$", dummyImplementation);

            string fileName = Path.Combine(projectDir, MainClassName + ".cs");
            File.WriteAllText(fileName, content);
        }

        private static void GenerateProjectFile(string projectDir, Benchmark benchmark)
        {
            var configuration = benchmark.Task.Configuration;
            var platform = configuration.Platform.ToConfig();
            var framework = configuration.Framework.ToConfig();

            var template = GetTemplate("BenchmarkCsproj.txt");
            var content = template.
                Replace("$Platform$", platform).
                Replace("$Framework$", framework).
                Replace("$TargetAssemblyReference$", GetReferenceToAssembly(benchmark.Target.Type)).
                Replace("$TargetMethodReturnTypeAssemblyReference$", GetReferenceToAssembly(benchmark.Target.Method.ReturnType));

            string fileName = Path.Combine(projectDir, MainClassName + ".csproj");
            File.WriteAllText(fileName, content);
        }

        private static string GetReferenceToAssembly(Type type)
        {
            var template = @"    <Reference Include=""$AssemblyName$"">
      <HintPath>..\$AssemblyFileName$</HintPath>
    </Reference>
  ";
            var assembly = type.Assembly;
            var fileName = new FileInfo(type.Assembly.Location).Name;
            return fileName == "mscorlib.dll"
                ? ""
                : template.
                    Replace("$AssemblyName$", assembly.GetName(false).Name).
                    Replace("$AssemblyFileName$", fileName);
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