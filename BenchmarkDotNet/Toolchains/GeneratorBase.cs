using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath, IConfig config)
        {
            ArtifactsPaths artifactsPaths = null;
            try
            {
                artifactsPaths = GetArtifactsPaths(benchmark, config, rootArtifactsFolderPath);

                Cleanup(artifactsPaths);

                CopyAllRequiredFiles(artifactsPaths);

                GenerateCode(benchmark, artifactsPaths);
                GenerateAppConfig(benchmark, artifactsPaths);
                GenerateProject(benchmark, artifactsPaths);
                GenerateBuildScript(benchmark, artifactsPaths);

                return GenerateResult.Success(artifactsPaths);
            }
            catch (Exception ex)
            {
                return GenerateResult.Failure(artifactsPaths, ex);
            }
        }

        protected abstract string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName);

        protected virtual string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath) => buildArtifactsDirectoryPath;

        protected virtual string GetCompilerPath(string rootArtifactsFolderPath) => string.Empty;

        protected virtual string GetProjectFilePath(string binariesDirectoryPath) => string.Empty;

        protected abstract void Cleanup(ArtifactsPaths artifactsPaths);

        protected abstract void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths);

        protected virtual void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths) { }

        protected abstract void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths);

        private ArtifactsPaths GetArtifactsPaths(Benchmark benchmark, IConfig config, string rootArtifactsFolderPath)
        {
            // its not ".cs" in order to avoid VS from displaying and compiling it with xprojs
            const string codeFileExtension = ".notcs";

            var programName = GetProgramName(benchmark, config);
            var buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(benchmark, programName);
            var binariesDirectoryPath = GetBinariesDirectoryPath(buildArtifactsDirectoryPath);
            var executablePath = Path.Combine(binariesDirectoryPath, $"{programName}{RuntimeInformation.ExecutableExtension}");

            return new ArtifactsPaths(
                cleanup: Cleanup,
                rootArtifactsFolderPath: rootArtifactsFolderPath,
                buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
                binariesDirectoryPath: binariesDirectoryPath,
                programCodePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{codeFileExtension}"),
                appConfigPath: $"{executablePath}.config",
                projectFilePath: GetProjectFilePath(buildArtifactsDirectoryPath),
                buildScriptFilePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{RuntimeInformation.ScriptFileExtension}"),
                executablePath: executablePath,
                compilerPath: GetCompilerPath(rootArtifactsFolderPath));
        }

        /// <summary>
        /// when config is set to KeepBenchmarkFiles we use benchmark.ShortInfo as name,
        /// otherwise (default) "BDN.Auto", mostly to prevent PathTooLongException
        /// </summary>
        private string GetProgramName(Benchmark benchmark, IConfig config)
        {
            const string shortName = "BDN.Auto";
            return config.KeepBenchmarkFiles ? benchmark.ShortInfo.Replace(' ', '_') : shortName;
        }

        private void GenerateCode(Benchmark benchmark, ArtifactsPaths artifactsPaths)
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

            File.WriteAllText(artifactsPaths.ProgramCodePath, content);
        }

        private void GenerateAppConfig(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
#if !RC1
            var sourcePath = benchmark.Target.Type.Assembly().Location + ".config";
#else
            var sourcePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + ".config";
#endif
            using (var source = File.Exists(sourcePath) ? new StreamReader(File.OpenRead(sourcePath)) : TextReader.Null)
            using (var destination = new System.IO.StreamWriter(File.Create(artifactsPaths.AppConfigPath), System.Text.Encoding.UTF8))
            {
                AppConfigGenerator.Generate(benchmark.Job, source, destination);
            }
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