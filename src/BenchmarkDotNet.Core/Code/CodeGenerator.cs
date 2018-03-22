﻿using System;
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
        internal static string Generate(Benchmark benchmark, IConfig config)
        {
            var provider = GetDeclarationsProvider(benchmark.Target);

            (bool useShadowCopy, string shadowCopyFolderPath) = GetShadowCopySettings();

            string passArguments = GetPassArguments(benchmark);

            string text = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkProgram.txt")).
                Replace("$OperationsPerInvoke$", provider.OperationsPerInvoke).
                Replace("$TargetTypeNamespace$", provider.TargetTypeNamespace).
                Replace("$TargetMethodReturnTypeNamespace$", provider.TargetMethodReturnTypeNamespace).
                Replace("$TargetTypeName$", provider.TargetTypeName).
                Replace("$TargetMethodDelegate$", provider.TargetMethodDelegate).
                Replace("$TargetMethodReturnType$", provider.TargetMethodReturnTypeName).
                Replace("$IdleMethodReturnTypeName$", provider.IdleMethodReturnTypeName).
                Replace("$GlobalSetupMethodName$", provider.GlobalSetupMethodName).
                Replace("$GlobalCleanupMethodName$", provider.GlobalCleanupMethodName).
                Replace("$IterationSetupMethodName$", provider.IterationSetupMethodName).
                Replace("$IterationCleanupMethodName$", provider.IterationCleanupMethodName).
                Replace("$IdleImplementation$", provider.IdleImplementation).
                Replace("$ExtraDefines$", provider.ExtraDefines).
                Replace("$ConsumeField$", provider.ConsumeField).
                Replace("$AdditionalLogic$", benchmark.Target.AdditionalLogic).
                Replace("$JobSetDefinition$", GetJobsSetDefinition(benchmark)).
                Replace("$ParamsContent$", GetParamsContent(benchmark)).
                Replace("$ArgumentsDefinition$", GetArgumentsDefinition(benchmark)).
                Replace("$DeclareArgumentFields$", GetDeclareArgumentFields(benchmark)).
                Replace("$InitializeArgumentFields$", GetInitializeArgumentFields(benchmark)).
                Replace("$LoadArguments$", GetLoadArguments(benchmark)).
                Replace("$PassArguments$", passArguments).
                Replace("$ExtraAttribute$", GetExtraAttributes(benchmark.Target)).
                Replace("$EngineFactoryType$", GetEngineFactoryTypeName(benchmark)).
                Replace("$ShadowCopyDefines$", useShadowCopy ? "#define SHADOWCOPY" : null).
                Replace("$ShadowCopyFolderPath$", shadowCopyFolderPath).
                Replace("$Ref$", provider.UseRefKeyword ? "ref" : null).
                Replace("$MeasureGcStats$", config.HasMemoryDiagnoser() ? "true" : "false").
                Replace("$DiassemblerEntryMethodName$", DisassemblerConstants.DiassemblerEntryMethodName).
                Replace("$TargetMethodCall$", provider.GetTargetMethodCall(passArguments)).
                ToString();

            text = Unroll(text, benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvResolver.Instance));

            return text;
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
        {
            if (target.Method.GetCustomAttributes(false).OfType<System.STAThreadAttribute>().Any())
            {
                return "[System.STAThreadAttribute]";
            }

            return string.Empty;
        }

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