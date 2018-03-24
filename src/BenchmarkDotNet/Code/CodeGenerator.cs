using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using RunMode = BenchmarkDotNet.Jobs.RunMode;

namespace BenchmarkDotNet.Code
{
    internal static class CodeGenerator
    {
        internal static string Generate(BuildPartition buildPartition)
        {
            (bool useShadowCopy, string shadowCopyFolderPath) = GetShadowCopySettings();

            var benchmarksCode = new List<string>(buildPartition.Benchmarks.Length);

            var extraDefines = new List<string>();
            var targetTypeNamespaces = new HashSet<string>();
            var targetMethodReturnTypeNamespace = new HashSet<string>();
            var additionalLogic = new HashSet<string>();

            foreach (var buildInfo in buildPartition.Benchmarks)
            {
                var benchmark = buildInfo.Benchmark;

                var provider = GetDeclarationsProvider(benchmark.Target);

                string passArguments = GetPassArguments(benchmark);

                extraDefines.Add($"{provider.ExtraDefines}_{buildInfo.Id}");

                AddNonEmptyUnique(targetTypeNamespaces, provider.TargetTypeNamespace);
                AddNonEmptyUnique(targetMethodReturnTypeNamespace, provider.TargetMethodReturnTypeNamespace);
                AddNonEmptyUnique(additionalLogic, benchmark.Target.AdditionalLogic);

                string benchmarkTypeCode = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkType.txt"))
                    .Replace("$ID$", buildInfo.Id.ToString())
                    .Replace("$OperationsPerInvoke$", provider.OperationsPerInvoke)
                    .Replace("$TargetTypeName$", provider.TargetTypeName)
                    .Replace("$TargetMethodDelegate$", provider.TargetMethodDelegate)
                    .Replace("$TargetMethodReturnType$", provider.TargetMethodReturnTypeName)
                    .Replace("$IdleMethodReturnTypeName$", provider.IdleMethodReturnTypeName)
                    .Replace("$GlobalSetupMethodName$", provider.GlobalSetupMethodName)
                    .Replace("$GlobalCleanupMethodName$", provider.GlobalCleanupMethodName)
                    .Replace("$IterationSetupMethodName$", provider.IterationSetupMethodName)
                    .Replace("$IterationCleanupMethodName$", provider.IterationCleanupMethodName)
                    .Replace("$IdleImplementation$", provider.IdleImplementation)
                    .Replace("$ConsumeField$", provider.ConsumeField)
                    .Replace("$JobSetDefinition$", GetJobsSetDefinition(benchmark))
                    .Replace("$ParamsContent$", GetParamsContent(benchmark))
                    .Replace("$ArgumentsDefinition$", GetArgumentsDefinition(benchmark))
                    .Replace("$DeclareArgumentFields$", GetDeclareArgumentFields(benchmark))
                    .Replace("$InitializeArgumentFields$", GetInitializeArgumentFields(benchmark)).Replace("$LoadArguments$", GetLoadArguments(benchmark))
                    .Replace("$PassArguments$", passArguments)
                    .Replace("$EngineFactoryType$", GetEngineFactoryTypeName(benchmark))
                    .Replace("$Ref$", provider.UseRefKeyword ? "ref" : null)
                    .Replace("$MeasureGcStats$", buildInfo.Config.HasMemoryDiagnoser() ? "true" : "false")
                    .Replace("$DiassemblerEntryMethodName$", DisassemblerConstants.DiassemblerEntryMethodName)
                    .Replace("$TargetMethodCall$", provider.GetTargetMethodCall(passArguments)).ToString();

                benchmarkTypeCode = Unroll(benchmarkTypeCode, benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvResolver.Instance));

                benchmarksCode.Add(benchmarkTypeCode);
            }

            string benchmarkProgramContent = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkProgram.txt"))
                .Replace("$ShadowCopyDefines$", useShadowCopy ? "#define SHADOWCOPY" : null).Replace("$ShadowCopyFolderPath$", shadowCopyFolderPath)
                .Replace("$ExtraDefines$", string.Join(Environment.NewLine, extraDefines))
                .Replace("$TargetTypeNamespace$", string.Join(Environment.NewLine, targetTypeNamespaces))
                .Replace("$TargetMethodReturnTypeNamespace$", string.Join(Environment.NewLine, targetMethodReturnTypeNamespace))
                .Replace("$AdditionalLogic$", string.Join(Environment.NewLine, additionalLogic))
                .Replace("$DerivedTypes$", string.Join(Environment.NewLine, benchmarksCode))
                .Replace("$ExtraAttribute$", GetExtraAttributes(buildPartition.RepresentativeBenchmark.Target))
                .ToString();

            return benchmarkProgramContent;
        }

        private static void AddNonEmptyUnique(HashSet<string> items, string value)
        {
            if (!string.IsNullOrEmpty(value) && !items.Contains(value))
                items.Add(value);
        }

        private static (bool, string) GetShadowCopySettings()
        {
            var benchmarkDotNetLocation = Path.GetDirectoryName(typeof(CodeGenerator).GetTypeInfo().Assembly.Location);

            if (benchmarkDotNetLocation != null && benchmarkDotNetLocation.ToUpper().Contains("LINQPAD"))
            {
                /* "LINQPad normally puts the compiled query into a different folder than the referenced assemblies 
                 * - this allows for optimizations to reduce file I/O, which is important in the scratchpad scenario"
                 * 
                 * so in case we detect we are running from LINQPad, we give a hint to assembly loading to search also in this folder
                 */

                return (true, benchmarkDotNetLocation);
            }

            return (false, string.Empty);
        }

        private static string Unroll(string text, int factor)
        {
            const string unrollDirective = "@Unroll@";
            const string dummyUnrollDirective = "@DummyUnroll@";
            const int dummyUnrollFactor = 1 << 6;
            var oldLines = text.Split('\n');
            var newLines = new List<string>();
            foreach (string line in oldLines)
            {
                if (line.Contains(unrollDirective))
                {
                    string newLine = line.Replace(unrollDirective, "");
                    for (int i = 0; i < factor; i++)
                        newLines.Add(newLine);
                }
                else if (line.Contains(dummyUnrollDirective))
                {
                    string newLine = line.Replace(dummyUnrollDirective, "");
                    for (int i = 0; i < dummyUnrollFactor; i++)
                        newLines.Add(newLine);
                }
                else
                    newLines.Add(line);
            }
            return string.Join("\n", newLines);
        }

        private static string GetJobsSetDefinition(Benchmark benchmark)
        {
            return CharacteristicSetPresenter.SourceCode.
                ToPresentation(benchmark.Job).
                Replace("; ", ";\n                ");
        }

        private static DeclarationsProvider GetDeclarationsProvider(Target target)
        {
            var method = target.Method;

            if (method.ReturnType == typeof(Task))
            {
                return new TaskDeclarationsProvider(target);
            }
            if (method.ReturnType.GetTypeInfo().IsGenericType
                && (method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>)
                    || method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                return new GenericTaskDeclarationsProvider(target);
            }

            if (method.ReturnType == typeof(void))
            {
                var isUsingAsyncKeyword = method.HasAttribute<AsyncStateMachineAttribute>();
                if (isUsingAsyncKeyword)
                {
                    throw new NotSupportedException("async void is not supported by design");
                }

                return new VoidDeclarationsProvider(target);
            }

            if (method.ReturnType.IsByRef)
            {
                return new ByRefDeclarationsProvider(target);
            }

            return new NonVoidDeclarationsProvider(target);
        }

        private static string GetParamsContent(Benchmark benchmark)
            => string.Join(
                string.Empty,
                benchmark.Parameters.Items
                    .Where(parameter => !parameter.IsArgument)
                    .Select(parameter => $"{(parameter.IsStatic ? "" : "instance.")}{parameter.Name} = {parameter.ToSourceCode()};"));

        private static string GetArgumentsDefinition(Benchmark benchmark)
            => string.Join(
                ", ",
                benchmark.Target.Method.GetParameters()
                         .Select((parameter, index) => $"{GetParameterModifier(parameter)} {parameter.ParameterType.GetCorrectCSharpTypeName()} arg{index}"));

        private static string GetDeclareArgumentFields(Benchmark benchmark)
            => string.Join(
                Environment.NewLine,
                benchmark.Target.Method.GetParameters()
                         .Select((parameter, index) => $"private {parameter.ParameterType.GetCorrectCSharpTypeName()} __argField{index};"));

        private static string GetInitializeArgumentFields(Benchmark benchmark)
            => string.Join(
                Environment.NewLine,
                benchmark.Target.Method.GetParameters()
                         .Select((parameter, index) => $"__argField{index} = {benchmark.Parameters.GetArgument(parameter.Name).ToSourceCode()};")); // we init the fields in ctor to provoke all possible allocations and overhead of other type

        private static string GetLoadArguments(Benchmark benchmark)
            => string.Join(
                Environment.NewLine,
                benchmark.Target.Method.GetParameters()
                         .Select((parameter, index) => $"{(parameter.ParameterType.IsByRef ? "ref" : string.Empty)} {parameter.ParameterType.GetCorrectCSharpTypeName()} arg{index} = {(parameter.ParameterType.IsByRef ? "ref" : string.Empty)} __argField{index};"));

        private static string GetPassArguments(Benchmark benchmark)
            => string.Join(
                ", ",
                benchmark.Target.Method.GetParameters()
                    .Select((parameter, index) => $"{GetParameterModifier(parameter)} arg{index}"));

        private static string GetExtraAttributes(Target target) 
            => target.Method.GetCustomAttributes(false).OfType<STAThreadAttribute>().Any() ? "[System.STAThreadAttribute]" : string.Empty;

        private static string GetEngineFactoryTypeName(Benchmark benchmark)
        {
            var factory = benchmark.Job.ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance);
            var factoryType = factory.GetType();

            if (!factoryType.GetTypeInfo().DeclaredConstructors.Any(ctor => ctor.IsPublic && !ctor.GetParameters().Any()))
            {
                throw new NotSupportedException("Custom factory must have a public parameterless constructor");
            }

            return factoryType.GetCorrectCSharpTypeName();
        }

        private static string GetParameterModifier(ParameterInfo parameterInfo)
            => parameterInfo.ParameterType.IsByRef
                ? "ref"
                : parameterInfo.IsOut
                    ? "out"
                    : string.Empty;

        private class SmartStringBuilder
        {
            private readonly string originalText;
            private readonly StringBuilder builder;

            public SmartStringBuilder(string text)
            {
                originalText = text;
                builder = new StringBuilder(text);
            }

            public SmartStringBuilder Replace(string oldValue, string newValue)
            {
                if (originalText.Contains(oldValue))
                    builder.Replace(oldValue, newValue);
                else
                    builder.Append($"\n// '{oldValue}' not found");
                return this;
            }

            public override string ToString() => builder.ToString();
        }
    }
}