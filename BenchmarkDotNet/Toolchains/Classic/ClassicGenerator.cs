using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class ClassicGenerator : IGenerator
    {
        public const string MainClassName = "Program";

        private static readonly HashSet<string> PredefinedAssemblies = new HashSet<string>(
            new[]
            {
                "mscorlib",
                "System",
                "System.Core",
                "System.Xml.Linq",
                "System.Xml"
            });

        public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger)
        {
            var result = CreateProjectDirectory(benchmark);
            GenerateProgramFile(result.DirectoryPath, benchmark);
            GenerateProjectFile(logger, result.DirectoryPath, benchmark);
            GenerateProjectBuildFile(result.DirectoryPath);
            GenerateAppConfigFile(result.DirectoryPath, benchmark.Job);
            return result;
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

            var targetTypeName = target.Type.FullName.Replace('+', '.');
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

            var paramsContent = string.Join(
                "",
                benchmark.Parameters.Items.Select(
                    parameter =>
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

            string fileName = Path.Combine(projectDir, MainClassName + ".cs");
            File.WriteAllText(fileName, content);
        }

        private string GetParameterValue(object value)
        {
            if (value is bool)
                return value.ToString().ToLower();
            if (value is string)
                return "\"" + value + "\"";
            if (value is float)
                return ((float)value).ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double)
                return ((double)value).ToString("G", CultureInfo.InvariantCulture) + "d";
            if (value is decimal)
                return ((decimal)value).ToString("G", CultureInfo.InvariantCulture) + "m";
            if (value.GetType().IsEnum())
                return value.GetType().FullName + "." + value;
            return value.ToString();
        }

        protected virtual void GenerateProjectFile(ILogger logger, string projectDir, Benchmark benchmark)
        {
#if !CLASSIC
            throw new InvalidOperationException("Must not be called");
#else
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

            // Ensure BenchmarkDotNet.dll is in the correct place (e.g. when running in LINQPad)
            EnsureDependancyInCorrectLocation(logger, typeof(BenchmarkAttribute), projectDir);

            EnsureDependancyInCorrectLocation(logger, benchmark.Target.Type, projectDir);
            EnsureDependancyInCorrectLocation(logger, benchmark.Target.Method.ReturnType, projectDir);
#endif
        }

        protected virtual void GenerateProjectBuildFile(string projectDir)
        {
            var content = ResourceHelper.LoadTemplate("BuildBenchmark.txt");
            string fileName = Path.Combine(projectDir, "BuildBenchmark.bat");
            File.WriteAllText(fileName, content);
        }

        private void GenerateAppConfigFile(string projectDir, IJob job)
        {
            var useLagacyJit = job.Jit == Jit.RyuJit
                || (job.Jit == Jit.Host && EnvironmentHelper.GetCurrentInfo().HasRyuJit)
                ? "0"
                : "1";

            var template =
                ResourceHelper.LoadTemplate(
                    job.Jit == Jit.Host ? "BenchmarkAppConfigEmpty.txt" : "BenchmarkAppConfig.txt");
            var content = template.
                Replace("$UseLagacyJit$", useLagacyJit);

            string fileName = Path.Combine(projectDir, "app.config");
            File.WriteAllText(fileName, content);
        }

        private GenerateResult CreateProjectDirectory(Benchmark benchmark)
        {
            var directoryPath = GetDirectoryPath(benchmark);
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

        protected virtual string GetDirectoryPath(Benchmark benchmark)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), benchmark.ShortInfo);
        }

        private void CopyFile(ILogger logger, string sourcePath, string destinationPath)
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

#if CLASSIC
        private void EnsureDependancyInCorrectLocation(ILogger logger, Type type, string outputDir)
        {
            var fileInfo = new FileInfo(type.Assembly.Location);
            if (fileInfo.Name == "mscorlib.dll")
                return;

            var expectedLocation = Path.GetFullPath(Path.Combine(outputDir, "..\\" + fileInfo.Name));
            if (File.Exists(expectedLocation) == false)
            {
                logger.WriteLineInfo("// File doesn't exist: {0}", expectedLocation);
                logger.WriteLineInfo("//   Actually at: {0}", fileInfo.FullName);
                CopyFile(logger, fileInfo.FullName, expectedLocation);
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

                buffer.Append($"<Reference Include=\"{assemblyName.Name}\">")
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
#endif
    }
}