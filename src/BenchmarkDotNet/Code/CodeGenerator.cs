using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
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
            var additionalLogic = new HashSet<string>();

            foreach (var buildInfo in buildPartition.Benchmarks)
            {
                var benchmark = buildInfo.BenchmarkCase;

                var provider = GetDeclarationsProvider(benchmark.Descriptor);

                string passArguments = GetPassArguments(benchmark);

                AddNonEmptyUnique(additionalLogic, benchmark.Descriptor.AdditionalLogic);

                string benchmarkTypeCode = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkType.txt"))
                    .Replace("$ID$", buildInfo.Id.ToString())
                    .Replace("$OperationsPerInvoke$", provider.OperationsPerInvoke)
                    .Replace("$WorkloadTypeName$", provider.WorkloadTypeName)
                    .Replace("$GlobalSetupMethodName$", provider.GlobalSetupMethodName)
                    .Replace("$GlobalCleanupMethodName$", provider.GlobalCleanupMethodName)
                    .Replace("$IterationSetupMethodName$", provider.IterationSetupMethodName)
                    .Replace("$IterationCleanupMethodName$", provider.IterationCleanupMethodName)
                    .Replace("$JobSetDefinition$", GetJobsSetDefinition(benchmark))
                    .Replace("$ParamsInitializer$", GetParamsInitializer(benchmark))
                    .Replace("$ParamsContent$", GetParamsContent(benchmark))
                    .Replace("$ArgumentsDefinition$", GetArgumentsDefinition(benchmark))
                    .Replace("$DeclareArgumentFields$", GetDeclareArgumentFields(benchmark))
                    .Replace("$InitializeArgumentFields$", GetInitializeArgumentFields(benchmark)).Replace("$LoadArguments$", GetLoadArguments(benchmark))
                    .Replace("$PassArguments$", passArguments)
                    .Replace("$EngineFactoryType$", GetEngineFactoryTypeName(benchmark))
                    .Replace("$MeasureExtraStats$", buildInfo.Config.HasExtraStatsDiagnoser() ? "true" : "false")
                    .Replace("$DisassemblerEntryMethodName$", DisassemblerConstants.DisassemblerEntryMethodName)
                    .Replace("$WorkloadMethodCall$", provider.GetWorkloadMethodCall(passArguments))
                    .Replace("$InProcessDiagnoserRouters$", GetInProcessDiagnoserRouters(buildInfo))
                    .ToString();

                benchmarkTypeCode = Unroll(benchmarkTypeCode, benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance));

                benchmarksCode.Add(benchmarkTypeCode);
            }

            if (buildPartition.IsNativeAot)
                extraDefines.Add("#define NATIVEAOT");
            else if (buildPartition.IsNetFramework)
                extraDefines.Add("#define NETFRAMEWORK");
            else if (buildPartition.IsWasm)
                extraDefines.Add("#define WASM");

            string benchmarkProgramContent = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkProgram.txt"))
                .Replace("$ShadowCopyDefines$", useShadowCopy ? "#define SHADOWCOPY" : null).Replace("$ShadowCopyFolderPath$", shadowCopyFolderPath)
                .Replace("$ExtraDefines$", string.Join(Environment.NewLine, extraDefines))
                .Replace("$AdditionalLogic$", string.Join(Environment.NewLine, additionalLogic))
                .Replace("$DerivedTypes$", string.Join(Environment.NewLine, benchmarksCode))
                .Replace("$ExtraAttribute$", GetExtraAttributes(buildPartition.RepresentativeBenchmarkCase.Descriptor))
                .Replace("$NativeAotSwitch$", GetNativeAotSwitch(buildPartition))
                .ToString();

            return benchmarkProgramContent;
        }

        private static void AddNonEmptyUnique(HashSet<string> items, string value)
        {
            if (!string.IsNullOrEmpty(value))
                items.Add(value);
        }

        private static (bool, string) GetShadowCopySettings()
        {
            string benchmarkDotNetLocation = Path.GetDirectoryName(typeof(CodeGenerator).GetTypeInfo().Assembly.Location);

            if (benchmarkDotNetLocation != null && benchmarkDotNetLocation.IndexOf("LINQPAD", StringComparison.OrdinalIgnoreCase) >= 0)
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
                else
                    newLines.Add(line);
            }
            return string.Join("\n", newLines);
        }

        private static string GetJobsSetDefinition(BenchmarkCase benchmarkCase)
        {
            return CharacteristicSetPresenter.SourceCode.
                ToPresentation(benchmarkCase.Job).
                Replace("; ", ";\n                ");
        }

        private static DeclarationsProvider GetDeclarationsProvider(Descriptor descriptor)
        {
            var method = descriptor.WorkloadMethod;

            if (method.ReturnType == typeof(Task) || method.ReturnType == typeof(ValueTask))
            {
                return new AsyncDeclarationsProvider(descriptor);
            }
            if (method.ReturnType.GetTypeInfo().IsGenericType
                && (method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>)
                    || method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                return new AsyncDeclarationsProvider(descriptor);
            }

            if (method.ReturnType == typeof(void) && method.HasAttribute<AsyncStateMachineAttribute>())
            {
                throw new NotSupportedException("async void is not supported by design");
            }

            return new SyncDeclarationsProvider(descriptor);
        }

        private static string GetParamsInitializer(BenchmarkCase benchmarkCase)
            => string.Join(
                ", ",
                benchmarkCase.Parameters.Items
                    .Where(parameter => !parameter.IsArgument && !parameter.IsStatic)
                    .Select(parameter => $"{parameter.Name} = default"));

        // internal for tests

        internal static string GetParamsContent(BenchmarkCase benchmarkCase)
            => string.Join(
                string.Empty,
                benchmarkCase.Parameters.Items
                    .Where(parameter => !parameter.IsArgument)
                    .Select(parameter => $"{(parameter.IsStatic ? "" : "instance.")}{parameter.Name} = {parameter.ToSourceCode()};"));

        private static string GetArgumentsDefinition(BenchmarkCase benchmarkCase)
            => string.Join(
                ", ",
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                         .Select((parameter, index) => $"{GetParameterModifier(parameter)} {parameter.ParameterType.GetCorrectCSharpTypeName()} arg{index}"));

        private static string GetDeclareArgumentFields(BenchmarkCase benchmarkCase)
            => string.Join(
                Environment.NewLine,
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                         .Select((parameter, index) => $"private {GetFieldType(parameter.ParameterType, benchmarkCase.Parameters.GetArgument(parameter.Name)).GetCorrectCSharpTypeName()} __argField{index};"));

        private static string GetInitializeArgumentFields(BenchmarkCase benchmarkCase)
            => string.Join(
                Environment.NewLine,
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                         .Select((parameter, index) => $"__argField{index} = {benchmarkCase.Parameters.GetArgument(parameter.Name).ToSourceCode()};")); // we init the fields in ctor to provoke all possible allocations and overhead of other type

        private static string GetLoadArguments(BenchmarkCase benchmarkCase)
            => string.Join(
                Environment.NewLine,
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                         .Select((parameter, index) => $"{(parameter.ParameterType.IsByRef ? "ref" : string.Empty)} {parameter.ParameterType.GetCorrectCSharpTypeName()} arg{index} = {(parameter.ParameterType.IsByRef ? "ref" : string.Empty)} __argField{index};"));

        private static string GetPassArguments(BenchmarkCase benchmarkCase)
            => string.Join(
                ", ",
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) => $"{GetParameterModifier(parameter)} arg{index}"));

        private static string GetExtraAttributes(Descriptor descriptor)
            => descriptor.WorkloadMethod.GetCustomAttributes(false).OfType<STAThreadAttribute>().Any() ? "[System.STAThreadAttribute]" : string.Empty;

        private static string GetEngineFactoryTypeName(BenchmarkCase benchmarkCase)
        {
            var factory = benchmarkCase.Job.ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance);
            var factoryType = factory.GetType();

            if (!factoryType.GetTypeInfo().DeclaredConstructors.Any(ctor => ctor.IsPublic && !ctor.GetParameters().Any()))
            {
                throw new NotSupportedException("Custom factory must have a public parameterless constructor");
            }

            return factoryType.GetCorrectCSharpTypeName();
        }

        private static string GetInProcessDiagnoserRouters(BenchmarkBuildInfo buildInfo)
        {
            var sourceCodes = buildInfo.CompositeInProcessDiagnoser.InProcessDiagnosers
                .Select((d, i) => ToSourceCode(d, buildInfo.BenchmarkCase, i))
                .WhereNotNull();
            return string.Join($",\n", sourceCodes);

            static string? ToSourceCode(IInProcessDiagnoser diagnoser, BenchmarkCase benchmarkCase, int index)
            {
                var handlerData = diagnoser.GetHandlerData(benchmarkCase);
                if (handlerData.HandlerType is null)
                {
                    return null;
                }
                string routerType = typeof(InProcessDiagnoserRouter).GetCorrectCSharpTypeName();
                return $$"""
                new {{routerType}}() {
                    {{nameof(InProcessDiagnoserRouter.handler)}} = {{routerType}}.{{nameof(InProcessDiagnoserRouter.Init)}}(new {{handlerData.HandlerType.GetCorrectCSharpTypeName()}}(), {{SourceCodeHelper.ToSourceCode(handlerData.SerializedConfig)}}),
                    {{nameof(InProcessDiagnoserRouter.index)}} = {{index}},
                    {{nameof(InProcessDiagnoserRouter.runMode)}} = {{SourceCodeHelper.ToSourceCode(diagnoser.GetRunMode(benchmarkCase))}}
                }
                """;
            }
        }

        private static string GetParameterModifier(ParameterInfo parameterInfo)
        {
            if (!parameterInfo.ParameterType.IsByRef)
                return string.Empty;

            // From https://stackoverflow.com/a/38110036/5852046 :
            // "If you don't do the IsByRef check for out parameters, then you'll incorrectly get members decorated with the
            // [Out] attribute from System.Runtime.InteropServices but which aren't actually C# out parameters."
            if (parameterInfo.IsOut)
                return "out";
            else if (parameterInfo.IsIn)
                return "in";
            else
                return "ref";
        }

        /// <summary>
        /// for NativeAOT we can't use reflection to load type and run a method, so we simply generate a switch for all types..
        /// </summary>
        private static string GetNativeAotSwitch(BuildPartition buildPartition)
        {
            if (!buildPartition.IsNativeAot)
                return default;

            var @switch = new StringBuilder(buildPartition.Benchmarks.Length * 30);
            @switch.AppendLine("switch (id) {");

            foreach (var buildInfo in buildPartition.Benchmarks)
                @switch.AppendLine($"case {buildInfo.Id.Value}: BenchmarkDotNet.Autogenerated.Runnable_{buildInfo.Id.Value}.Run(host, benchmarkName, diagnoserRunMode); break;");

            @switch.AppendLine("default: throw new System.NotSupportedException(\"invalid benchmark id\");");
            @switch.AppendLine("}");

            return @switch.ToString();
        }

        private static Type GetFieldType(Type argumentType, ParameterInstance argument)
        {
            // #774 we can't store Span in a field, so we store an array (which is later casted to Span when we load the arguments)
            if (argumentType.IsStackOnlyWithImplicitCast(argument.Value))
                return argument.Value.GetType();

            return argumentType;
        }

        private class SmartStringBuilder
        {
            private readonly string originalText;
            private readonly StringBuilder builder;

            public SmartStringBuilder(string text)
            {
                originalText = text;
                builder = new StringBuilder(text);
            }

            public SmartStringBuilder Replace(string oldValue, string? newValue)
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