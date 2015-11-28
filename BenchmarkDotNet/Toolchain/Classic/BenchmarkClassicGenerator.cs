using System;
using System.IO;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet.Toolchain.Classic
{
    internal class BenchmarkClassicGenerator : IBenchmarkGenerator
    {
        public const string MainClassName = "Program";
        private readonly IBenchmarkLogger logger;

        public BenchmarkClassicGenerator(IBenchmarkLogger logger)
        {
            this.logger = logger;
        }

        public BenchmarkGenerateResult GenerateProject(Benchmark benchmark)
        {
            var result = CreateProjectDirectory(benchmark);
            GenerateProgramFile(result.DirectoryPath, benchmark);
            GenerateProjectFile(result.DirectoryPath, benchmark);
            GenerateProjectBuildFile(result.DirectoryPath);
            GenerateAppConfigFile(result.DirectoryPath, benchmark.Task.Configuration);
            return result;
        }

        private void GenerateProgramFile(string projectDir, Benchmark benchmark)
        {
            var isVoid = benchmark.Target.Method.ReturnType == typeof(void);

            var operationsPerInvoke = benchmark.Target.OperationsPerInvoke;

            var targetTypeNamespace = string.IsNullOrWhiteSpace(benchmark.Target.Type.Namespace)
                ? ""
                : string.Format("using {0};", benchmark.Target.Type.Namespace);

            var targetMethodReturnTypeNamespace = string.Format("using {0};",
                    benchmark.Target.Method.ReturnType == typeof(void)
                        ? "System"
                        : benchmark.Target.Method.ReturnType.Namespace);

            var targetTypeName = benchmark.Target.Type.FullName.Replace('+', '.');
            var targetMethodName = benchmark.Target.Method.Name;

            var targetMethodReturnType = isVoid
                ? "void"
                : benchmark.Target.Method.ReturnType.GetCorrectTypeName();
            var targetMethodResultHolder = isVoid
                ? ""
                : $"private {targetMethodReturnType} value;";
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
            if (!benchmark.Task.ParametersSets.IsEmpty())
            {
                var typeQualifier = benchmark.Task.ParametersSets.IsStatic
                    ? $"{benchmark.Target.Type.Name}"
                    : "instance";
                paramsContent = $"{typeQualifier}.{benchmark.Task.ParametersSets.ParamFieldOrProperty} = BenchmarkParameters.ParseArgs(args).IntParam;";
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

            var targetBenchmarkTaskArguments =
                $"{benchmark.Task.ProcessCount}, " +
                $"{nameof(BenchmarkTask.Configuration).ToCamelCase()}: new {nameof(BenchmarkConfiguration)}({benchmark.Task.Configuration.ToCtorDefinition()}), " +
                $"{nameof(BenchmarkTask.ParametersSets).ToCamelCase()}: new {nameof(BenchmarkParametersSets)}({benchmark.Task.ParametersSets.ToCtorDefinition()})";

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
                Replace("$AdditionalLogic$", benchmark.Target.AdditionalLogic).
                Replace("$TargetBenchmarkTaskArguments$", targetBenchmarkTaskArguments).
                Replace("$ParamsContent$", paramsContent);

            string fileName = Path.Combine(projectDir, MainClassName + ".cs");
            File.WriteAllText(fileName, content);
        }

        private void GenerateProjectFile(string projectDir, Benchmark benchmark)
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

            // Ensure BenchmarkDotNet.dll is in the correct place (e.g. when running in LINQPad)
            EnsureDependancyInCorrectLocation(typeof(BenchmarkAttribute), projectDir);

            EnsureDependancyInCorrectLocation(benchmark.Target.Type, projectDir);
            EnsureDependancyInCorrectLocation(benchmark.Target.Method.ReturnType, projectDir);
        }

        private void GenerateProjectBuildFile(string projectDir)
        {
            var content = GetTemplate("BuildBenchmark.txt");
            string fileName = Path.Combine(projectDir, "BuildBenchmark.bat");
            File.WriteAllText(fileName, content);
        }

        private static string GetReferenceToAssembly(Type type)
        {
            var template = @"    <Reference Include=""$AssemblyName$"">
      <HintPath>..\$AssemblyFileName$</HintPath>
    </Reference>";
            var assembly = type.Assembly;
            var fileName = new FileInfo(type.Assembly.Location).Name;
            return fileName == "mscorlib.dll"
                ? ""
                : template.
                    Replace("$AssemblyName$", assembly.GetName(false).Name).
                    Replace("$AssemblyFileName$", fileName);
        }

        private void GenerateAppConfigFile(string projectDir, BenchmarkConfiguration configuration)
        {
            var useLagacyJit = configuration.JitVersion.ToConfig();

            var template = GetTemplate(configuration.JitVersion == BenchmarkJitVersion.HostJit ? "BenchmarkAppConfigEmpty.txt" : "BenchmarkAppConfig.txt");
            var content = template.
                Replace("$UseLagacyJit$", useLagacyJit);

            string fileName = Path.Combine(projectDir, "app.config");
            File.WriteAllText(fileName, content);
        }

        private static BenchmarkGenerateResult CreateProjectDirectory(Benchmark benchmark)
        {
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), benchmark.Caption);
            bool exist = Directory.Exists(directoryPath);
            Exception deleteException = null;
            for (int attempt = 0; attempt < 3 && exist; attempt++)
            {
                if (attempt != 0)
                    Thread.Sleep(500); // Previous benchmark run didn't release some files
                try
                {
                    Directory.Delete(directoryPath, true);
                    exist = Directory.Exists(directoryPath);
                }
                catch (Exception e)
                {
                    // Can't delete the directory =(
                    deleteException = e;
                }
            }
            if (exist)
                return new BenchmarkGenerateResult(directoryPath, false, deleteException);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return new BenchmarkGenerateResult(directoryPath, true, null);
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

        private void EnsureDependancyInCorrectLocation(Type type, string outputDir)
        {
            var fileInfo = new FileInfo(type.Assembly.Location);
            if (fileInfo.Name == "mscorlib.dll")
                return;

            var expectedLocation = Path.GetFullPath(Path.Combine(outputDir, "..\\" + fileInfo.Name));
            if (File.Exists(expectedLocation) == false)
            {
                logger.WriteLineInfo("// File doesn't exist: {0}", expectedLocation);
                logger.WriteLineInfo("//   Actually at: {0}", fileInfo.FullName);
                CopyFile(fileInfo.FullName, expectedLocation);
            }
        }

        private void CopyFile(string sourcePath, string destinationPath)
        {
            logger.WriteLineInfo("//   Copying {0}", Path.GetFileName(sourcePath));
            logger.WriteLineInfo("//   from: {0}", Path.GetDirectoryName(sourcePath));
            logger.WriteLineInfo("//   to: {0}", Path.GetDirectoryName(destinationPath));
            try
            {
                File.Copy(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), overwrite: true);
            }
            catch (Exception ex)
            {
                logger.WriteLineError(ex.Message);
                throw;
            }
        }
    }
}