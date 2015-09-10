using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using BenchmarkDotNet.Logging;

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

        public BuildResult BuildProject(string directoryPath, IBenchmarkLogger logger)
        {
            string projectFileName = Path.Combine(directoryPath, MainClassName + ".csproj");
            var consoleLogger = new MSBuildConsoleLogger(logger);
            var globalProperties = new Dictionary<string, string>();
            var buildRequest = new BuildRequestData(projectFileName, globalProperties, null, new[] { "Build" }, null);
            var buildParameters = new BuildParameters(new ProjectCollection()) { DetailedSummary = false, Loggers = new ILogger[] { consoleLogger } };
            var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
            return buildResult;
        }

        private static void GenerateProgramFile(string projectDir, Benchmark benchmark)
        {
            var isVoid = benchmark.Target.Method.ReturnType == typeof(void);

            var operationsPerInvoke = benchmark.Target.OperationsPerInvoke;

            var targetTypeNamespace = benchmark.Target.Type.Namespace;
            var targetMethodReturnTypeNamespace = benchmark.Target.Method.ReturnType == typeof(void)
                ? "System"
                : benchmark.Target.Method.ReturnType.Namespace;

            var targetTypeName = benchmark.Target.Type.FullName.Replace('+', '.');
            var targetMethodName = benchmark.Target.Method.Name;

            var targetMethodReturnType = isVoid
                ? "void"
                : benchmark.Target.Method.ReturnType.GetCorrectTypeName();
            var targetMethodResultHolder = isVoid
                ? "" :
                $"private {targetMethodReturnType} value;";
            var targetMethodHoldValue = isVoid
                ? ""
                : "value = ";
            var targetMethodDelegateType = isVoid
                ? "Action "
                : $"Func<{targetMethodReturnType}> ";

            // setupMethod is optional, so default to an empty delegate, so there is always something that can be invoked
            var setupMethodName = benchmark.Target.SetupMethod != null
                ? benchmark.Target.SetupMethod.Name
                : "() => { }";

            var idleImplementation = isVoid
                ? ""
                : $"return default({targetMethodReturnType});";

            var paramsContent = "";
            if (benchmark.Task.Params != null)
            {
                var typeQualifier = benchmark.Task.Params.IsStatic
                    ? $"{benchmark.Target.Type.Name}"
                    : "instance";
                paramsContent = $"{typeQualifier}.{benchmark.Task.Params.ParamFieldOrProperty} = BenchmarkParams.Parse(args);";
            }

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
                Replace("$OperationsPerInvoke$", operationsPerInvoke.ToInvariantString()).
                Replace("$TargetTypeNamespace$", targetTypeNamespace).
                Replace("$TargetMethodReturnTypeNamespace$", targetMethodReturnTypeNamespace).
                Replace("$TargetTypeName$", targetTypeName).
                Replace("$TargetMethodName$", targetMethodName).
                Replace("$TargetMethodResultHolder$", targetMethodResultHolder).
                Replace("$TargetMethodDelegateType$", targetMethodDelegateType).
                Replace("$TargetMethodHoldValue$", targetMethodHoldValue).
                Replace("$TargetMethodReturnType$", targetMethodReturnType).
                Replace("$SetupMethodName$", setupMethodName).
                Replace("$IdleImplementation$", idleImplementation).
                Replace("$AdditionalLogic$", benchmark.Target.AdditionalLogic)
               .Replace("$ParamsContent$", paramsContent);

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

            var template = GetTemplate(configuration.JitVersion == BenchmarkJitVersion.HostJit ? "BenchmarkAppConfigEmpty.txt" : "BenchmarkAppConfig.txt");
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