using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    internal abstract class GeneratorBase : IGenerator
    {
        protected const string ShortFolderName = "BDN.Auto";

        protected const string ProgramFileName = "Program.notcs";

        internal static string BuildBenchmarkScriptFileName => "BuildBenchmark" + RuntimeInformation.ScriptFileExtension;

        public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath, IConfig config)
        {
            var result = CreateProjectDirectory(benchmark, rootArtifactsFolderPath, config);

            CopyAllRequiredFiles(rootArtifactsFolderPath, result.DirectoryPath, benchmark);

            GenerateProgramFile(result.DirectoryPath, benchmark);
            GenerateProjectFile(logger, result.DirectoryPath, benchmark);

            var appConfigPath = GenerateAppConfigFile(result.DirectoryPath, benchmark, config);
            GenerateProjectBuildFile(Path.Combine(result.DirectoryPath, BuildBenchmarkScriptFileName), benchmark, rootArtifactsFolderPath, appConfigPath);

            return result;
        }

        protected virtual void CopyAllRequiredFiles(string rootArtifactsFolderPath, string binariesDirectoryPath, Benchmark benchmark) { }

        protected abstract string GetBinariesDirectoryPath(Benchmark benchmark, string rootArtifactsFolderPath, IConfig config);

        protected virtual void GenerateProjectFile(ILogger logger, string projectDir, Benchmark benchmark) { }

        protected abstract void GenerateProjectBuildFile(string scriptFilePath, Benchmark benchmark, string rootArtifactsFolderPath, string appConfigPath);

        protected virtual string GetProgramName(Benchmark benchmark, IConfig config) => "Program";

        private GenerateResult CreateProjectDirectory(Benchmark benchmark, string rootArtifactsFolderPath, IConfig config)
        {
            var directoryPath = GetBinariesDirectoryPath(benchmark, rootArtifactsFolderPath, config);
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
                catch (DirectoryNotFoundException)
                {
                    exist = false;
                    break;
                }
                catch (Exception e)
                {
                    // Can't delete the directory =(
                    deleteException = e;
                }
            }
            if (exist)
                return new GenerateResult(directoryPath, false, deleteException);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return new GenerateResult(directoryPath, true, null);
        }

        private void GenerateProgramFile(string projectDir, Benchmark benchmark)
        {
            var target = benchmark.Target;
            var isVoid = target.Method.ReturnType == typeof(void);

            var operationsPerInvoke = target.OperationsPerInvoke;

            var targetTypeNamespace = string.IsNullOrWhiteSpace(target.Type.Namespace)
                ? ""
                : $"using {target.Type.Namespace};";

            // As "using System;" is always included in the template, don't emit it again
            var emptyReturnTypeNamespace = target.Method.ReturnType == typeof(void) ||
                target.Method.ReturnType.Namespace == "System" ||
                string.IsNullOrWhiteSpace(target.Method.ReturnType.Namespace);
            var targetMethodReturnTypeNamespace = emptyReturnTypeNamespace
                ? ""
                : $"using {target.Method.ReturnType.Namespace};";

            var targetTypeName = target.Type.GetCorrectTypeName();
            var targetMethodName = target.Method.Name;

            var targetMethodReturnType = isVoid
                ? "void"
                : target.Method.ReturnType.GetCorrectTypeName();
            var idleMethodReturnType = isVoid || !target.Method.ReturnType.IsValueType()
                ? targetMethodReturnType
                : "int";
            var targetMethodResultHolder = isVoid
                ? ""
                : $"private {targetMethodReturnType} value;";
            var targetMethodHoldValue = isVoid
                ? ""
                : "value = ";
            var targetMethodDelegateType = isVoid
                ? "Action "
                : $"Func<{targetMethodReturnType}> ";
            var idleMethodDelegateType = isVoid
                ? "Action "
                : $"Func<{idleMethodReturnType}> ";

            // setupMethod is optional, so default to an empty delegate, so there is always something that can be invoked
            var setupMethodName = target.SetupMethod != null
                ? target.SetupMethod.Name
                : "() => { }";

            var idleImplementation = isVoid
                ? ""
                : $"return {(target.Method.ReturnType.IsValueType() ? "0" : "null")};";

            var paramsContent = string.Join("", benchmark.Parameters.Items.Select(parameter =>
                $"{(parameter.IsStatic ? "" : "instance.")}{parameter.Name} = {GetParameterValue(parameter.Value)};"));

            var targetBenchmarkTaskArguments = benchmark.Job.GenerateWithDefinitions();

            var contentTemplate = ResourceHelper.LoadTemplate("BenchmarkProgram.txt");
            var content = contentTemplate.
                Replace("$OperationsPerInvoke$", operationsPerInvoke.ToString()).
                Replace("$TargetTypeNamespace$", targetTypeNamespace).
                Replace("$TargetMethodReturnTypeNamespace$", targetMethodReturnTypeNamespace).
                Replace("$TargetTypeName$", targetTypeName).
                Replace("$TargetMethodName$", targetMethodName).
                Replace("$TargetMethodResultHolder$", targetMethodResultHolder).
                Replace("$TargetMethodDelegateType$", targetMethodDelegateType).
                Replace("$TargetMethodHoldValue$", targetMethodHoldValue).
                Replace("$TargetMethodReturnType$", targetMethodReturnType).
                Replace("$IdleMethodDelegateType$", idleMethodDelegateType).
                Replace("$IdleMethodReturnType$", idleMethodReturnType).
                Replace("$SetupMethodName$", setupMethodName).
                Replace("$IdleImplementation$", idleImplementation).
                Replace("$AdditionalLogic$", target.AdditionalLogic).
                Replace("$TargetBenchmarkTaskArguments$", targetBenchmarkTaskArguments).
                Replace("$ParamsContent$", paramsContent);

            File.WriteAllText(Path.Combine(projectDir, ProgramFileName), content);
        }

        private string GenerateAppConfigFile(string projectDir, Benchmark benchmark, IConfig config)
        {
#if !RC1
            var sourcePath = benchmark.Target.Type.Assembly().Location + ".config";
#else
            var sourcePath = Process.GetCurrentProcess().MainModule.FileName + ".config";
#endif
            var destinationPath = Path.Combine(projectDir, $"{GetProgramName(benchmark, config)}.exe.config");

            using (var source = File.Exists(sourcePath) ? new StreamReader(File.OpenRead(sourcePath)) : TextReader.Null)
            using (var destination = new System.IO.StreamWriter(File.Create(destinationPath), System.Text.Encoding.UTF8))
            {
                AppConfigGenerator.Generate(benchmark.Job, source, destination);
            }

            return destinationPath;
        }

        private string GetParameterValue(object value)
        {
            if (value is bool)
                return value.ToString().ToLower();
            if (value is string)
                return $"\"{value}\"";
            if (value is char)
                return $"'{value}'";
            if (value is float)
                return ((float)value).ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double)
                return ((double)value).ToString("G", CultureInfo.InvariantCulture) + "d";
            if (value is decimal)
                return ((decimal)value).ToString("G", CultureInfo.InvariantCulture) + "m";
            if (value.GetType().IsEnum())
                return value.GetType().GetCorrectTypeName() + "." + value;
            return value.ToString();
        }
    }
}