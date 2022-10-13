using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
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

                string compilationId = $"{provider.ReturnsDefinition}_{buildInfo.Id}";

                AddNonEmptyUnique(additionalLogic, benchmark.Descriptor.AdditionalLogic);

                string benchmarkTypeCode = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkType.txt"))
                    .Replace("$ID$", buildInfo.Id.ToString())
                    .Replace("$OperationsPerInvoke$", provider.OperationsPerInvoke)
                    .Replace("$WorkloadTypeName$", provider.WorkloadTypeName)
                    .Replace("$WorkloadMethodDelegate$", provider.WorkloadMethodDelegate(passArguments))
                    .Replace("$WorkloadMethodReturnType$", provider.WorkloadMethodReturnTypeName)
                    .Replace("$WorkloadMethodReturnTypeModifiers$", provider.WorkloadMethodReturnTypeModifiers)
                    .Replace("$OverheadMethodReturnTypeName$", provider.OverheadMethodReturnTypeName)
                    .Replace("$GlobalSetupMethodName$", provider.GlobalSetupMethodName)
                    .Replace("$GlobalCleanupMethodName$", provider.GlobalCleanupMethodName)
                    .Replace("$IterationSetupMethodName$", provider.IterationSetupMethodName)
                    .Replace("$IterationCleanupMethodName$", provider.IterationCleanupMethodName)
                    .Replace("$OverheadImplementation$", provider.OverheadImplementation)
                    .Replace("$ConsumeField$", provider.ConsumeField)
                    .Replace("$JobSetDefinition$", GetJobsSetDefinition(benchmark))
                    .Replace("$ParamsContent$", GetParamsContent(benchmark))
                    .Replace("$ArgumentsDefinition$", GetArgumentsDefinition(benchmark))
                    .Replace("$DeclareArgumentFields$", GetDeclareArgumentFields(benchmark))
                    .Replace("$InitializeArgumentFields$", GetInitializeArgumentFields(benchmark)).Replace("$LoadArguments$", GetLoadArguments(benchmark))
                    .Replace("$PassArguments$", passArguments)
                    .Replace("$EngineFactoryType$", GetEngineFactoryTypeName(benchmark))
                    .Replace("$MeasureExtraStats$", buildInfo.Config.HasExtraStatsDiagnoser() ? "true" : "false")
                    .Replace("$DisassemblerEntryMethodName$", DisassemblerConstants.DisassemblerEntryMethodName)
                    .Replace("$WorkloadMethodCall$", provider.GetWorkloadMethodCall(passArguments))
                    .RemoveRedundantIfDefines(compilationId);

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
            if (!string.IsNullOrEmpty(value) && !items.Contains(value))
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
            const string dummyUnrollDirective = "@DummyUnroll@";
            const int dummyUnrollFactor = 1 << 6;
            string dummyUnrolled = string.Join("", Enumerable.Repeat("dummyVar++;", dummyUnrollFactor));
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
                    newLines.Add(line.Replace(dummyUnrollDirective, dummyUnrolled));
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
                return new TaskDeclarationsProvider(descriptor);
            }
            if (method.ReturnType.GetTypeInfo().IsGenericType
                && (method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>)
                    || method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                return new GenericTaskDeclarationsProvider(descriptor);
            }

            if (method.ReturnType == typeof(void))
            {
                bool isUsingAsyncKeyword = method.HasAttribute<AsyncStateMachineAttribute>();
                if (isUsingAsyncKeyword)
                {
                    throw new NotSupportedException("async void is not supported by design");
                }

                return new VoidDeclarationsProvider(descriptor);
            }

            if (method.ReturnType.IsByRef)
            {
                // System.Runtime.CompilerServices.IsReadOnlyAttribute is part of .NET Standard 2.1, we can't use it here..
                if (method.ReturnParameter.GetCustomAttributes().Any(attribute => attribute.GetType().Name == "IsReadOnlyAttribute"))
                    return new ByReadOnlyRefDeclarationsProvider(descriptor);
                else
                    return new ByRefDeclarationsProvider(descriptor);
            }

            return new NonVoidDeclarationsProvider(descriptor);
        }

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
                @switch.AppendLine($"case {buildInfo.Id.Value}: BenchmarkDotNet.Autogenerated.Runnable_{buildInfo.Id.Value}.Run(host, benchmarkName); break;");

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

            public SmartStringBuilder Replace(string oldValue, string newValue)
            {
                if (originalText.Contains(oldValue))
                    builder.Replace(oldValue, newValue);
                else
                    builder.Append($"\n// '{oldValue}' not found");
                return this;
            }

            public string RemoveRedundantIfDefines(string id)
            {
                var oldLines = builder.ToString().Split('\n');
                var newLines = new List<string>();
                bool keepAdding = true;

                foreach (string line in oldLines)
                {
                    if (line.StartsWith("#if RETURNS") || line.StartsWith("#elif RETURNS"))
                    {
                        keepAdding = line.Contains(id);
                    }
                    else if (line.StartsWith("#endif // RETURNS"))
                    {
                        keepAdding = true;
                    }
                    else if (keepAdding)
                    {
                        newLines.Add(line);
                    }
                }

                return string.Join("\n", newLines);
            }

            public override string ToString() => builder.ToString();
        }
    }
}