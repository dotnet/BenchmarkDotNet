using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
        internal static string Generate(BuildPartition buildPartition, CodeGenEntryPointType entryPointType, CodeGenBenchmarkRunCallType benchmarkRunCallType)
        {
            (bool useShadowCopy, string shadowCopyFolderPath) = GetShadowCopySettings();

            var benchmarksCode = new List<string>(buildPartition.Benchmarks.Length);

            foreach (var buildInfo in buildPartition.Benchmarks)
            {
                var benchmark = buildInfo.BenchmarkCase;

                var declarationsProvider = GetDeclarationsProvider(benchmark);
                var extraFields = declarationsProvider.GetExtraFields();

                string benchmarkTypeCode = declarationsProvider
                    .ReplaceTemplate(new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkType.txt")))
                    .Replace("$ID$", buildInfo.Id.ToString())
                    .Replace("$JobSetDefinition$", GetJobsSetDefinition(benchmark))
                    .Replace("$ParamsContent$", GetParamsContent(benchmark))
                    .Replace("$ArgumentsDefinition$", GetArgumentsDefinition(benchmark))
                    .Replace("$DeclareFieldsContainer$", GetDeclareFieldsContainer(benchmark, buildInfo.Id, extraFields))
                    .Replace("$InitializeArgumentFields$", GetInitializeArgumentFields(benchmark))
                    .Replace("$EngineFactoryType$", GetEngineFactoryTypeName(benchmark))
                    .Replace("$RunExtraIteration$", buildInfo.Config.HasExtraIterationDiagnoser(benchmark) ? "true" : "false")
                    .Replace("$DisassemblerEntryMethodName$", DisassemblerConstants.DisassemblerEntryMethodName)
                    .Replace("$InProcessDiagnoserRouters$", GetInProcessDiagnoserRouters(buildInfo))
                    .ToString();

                benchmarkTypeCode = Unroll(benchmarkTypeCode, benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance));

                benchmarksCode.Add(benchmarkTypeCode);
            }

            string benchmarkProgramContent = new SmartStringBuilder(ResourceHelper.LoadTemplate("BenchmarkProgram.txt"))
                .Replace("$EntryPoint$", GetEntryPoint(buildPartition, entryPointType, useShadowCopy, shadowCopyFolderPath))
                .Replace("$BenchmarkRunCall$", GetBenchmarkRunCall(buildPartition, benchmarkRunCallType))
                .Replace("$DerivedTypes$", string.Join(Environment.NewLine, benchmarksCode))
                .ToString();

            return benchmarkProgramContent;
        }

        private static void AddNonEmptyUnique(HashSet<string> items, string value)
        {
            if (value.IsNotBlank())
                items.Add(value);
        }

        private static (bool, string) GetShadowCopySettings()
        {
            string benchmarkDotNetLocation = Path.GetDirectoryName(typeof(CodeGenerator).GetTypeInfo().Assembly.Location)!;

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

        private static DeclarationsProvider GetDeclarationsProvider(BenchmarkCase benchmark)
        {
            var method = benchmark.Descriptor.WorkloadMethod;

            if (method.ReturnType.IsAwaitable())
            {
                return new AsyncDeclarationsProvider(benchmark);
            }

            if (method.ReturnType == typeof(void) && method.HasAttribute<AsyncStateMachineAttribute>())
            {
                throw new NotSupportedException("async void is not supported by design");
            }

            return new SyncDeclarationsProvider(benchmark);
        }

        // internal for tests

        internal static string GetParamsContent(BenchmarkCase benchmarkCase)
            => string.Join(
                string.Empty,
                benchmarkCase.Parameters.Items
                    .Where(parameter => !parameter.IsArgument)
                    .Select(parameter => $"{(parameter.IsStatic ? benchmarkCase.Descriptor.Type.GetCorrectCSharpTypeName() : "base")}.{parameter.Name} = {parameter.ToSourceCode()};"));

        private static string GetArgumentsDefinition(BenchmarkCase benchmarkCase)
            => string.Join(
                ", ",
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) => $"{GetParameterModifier(parameter)} {parameter.ParameterType.GetCorrectCSharpTypeName()} arg{index}"));

        private static string GetDeclareFieldsContainer(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, string[] extraFields)
        {
            var fields = benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                .Select((parameter, index) => $"public {GetFieldType(parameter.ParameterType, benchmarkCase.Parameters.GetArgument(parameter.Name!)).GetCorrectCSharpTypeName()} argField{index};")
                .Concat(extraFields)
                .ToArray();

            // Prevent CS0169
            if (fields.Length == 0)
            {
                return string.Empty;
            }

            // Wrapper struct is necessary because of error CS4004: Cannot await in an unsafe context
            var sb = new StringBuilder();
            sb.AppendLine("""
                    [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
                    private unsafe struct FieldsContainer
                    {
            """);
            foreach (var field in fields)
            {
                sb.AppendLine($"            {field}");
            }
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine($"        private global::BenchmarkDotNet.Autogenerated.Runnable_{benchmarkId.Value}.FieldsContainer __fieldsContainer;");
            return sb.ToString();
        }

        /*
         
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        private unsafe struct FieldsContainer
        {
            $DeclareArgumentFields$
            $ExtraFields$
        }

        private global::BenchmarkDotNet.Autogenerated.Runnable_$ID$.FieldsContainer __fieldsContainer;
        
         */

        private static string GetInitializeArgumentFields(BenchmarkCase benchmarkCase)
            => string.Join(
                Environment.NewLine,
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) => $"this.__fieldsContainer.argField{index} = {benchmarkCase.Parameters.GetArgument(parameter.Name!).ToSourceCode()};")); // we init the fields in ctor to provoke all possible allocations and overhead of other type

        private static string GetEngineFactoryTypeName(BenchmarkCase benchmarkCase)
        {
            var factory = benchmarkCase.Job.ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance)!;
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

        internal static string GetParameterModifier(ParameterInfo parameterInfo)
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

        private static string GetEntryPoint(BuildPartition buildPartition, CodeGenEntryPointType entryPointType, bool useShadowCopy, string shadowCopyFolderPath)
        {
            if (entryPointType == CodeGenEntryPointType.Asynchronous)
            {
                // Only wasm uses async entry-point, we don't need to worry about .Net Framework assembly resolve helper.
                // Async entry-points also cannot participate in STAThread, so we ignore that as well.
                return """
                public static async global::System.Threading.Tasks.Task<System.Int32> Main(global::System.String[] args)
                        {
                            return await MainCore(args);
                        }
                """;
            }

            string mainImpl = """
            global::BenchmarkDotNet.Engines.BenchmarkSynchronizationContext benchmarkSynchronizationContext = global::BenchmarkDotNet.Engines.BenchmarkSynchronizationContext.CreateAndSetCurrent();
                        try
                        {
                            global::System.Threading.Tasks.ValueTask<System.Int32> task = MainCore(args);
                            return benchmarkSynchronizationContext.ExecuteUntilComplete(task);
                        }
                        finally
                        {
                            benchmarkSynchronizationContext.Dispose();
                        }
            """;

            if (!buildPartition.IsNetFramework)
            {
                return $$"""
                {{GetSTAThreadAttribute()}}
                        public static global::System.Int32 Main(global::System.String[] args)
                        {
                            {{mainImpl}}
                        }
                """;
            }

            return $$"""
            {{GetAssemblyResolveHelperClass()}}

                    {{GetSTAThreadAttribute()}}
                    public static global::System.Int32 Main(global::System.String[] args)
                    {
                        // this method MUST NOT have any dependencies to BenchmarkDotNet and any other external dlls!
                        // otherwise if LINQPad's shadow copy is enabled, we will not register for AssemblyLoading event
                        // before .NET Framework tries to load it for this method
                        using(new BenchmarkDotNet.Autogenerated.UniqueProgramName.DirtyAssemblyResolveHelper())
                            return AfterAssemblyLoadingAttached(args);
                    }

                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    private static global::System.Int32 AfterAssemblyLoadingAttached(global::System.String[] args)
                    {
                        {{mainImpl}}
                    }
            """;

            string GetSTAThreadAttribute()
                => buildPartition.RepresentativeBenchmarkCase.Descriptor.WorkloadMethod.GetCustomAttributes(false).OfType<STAThreadAttribute>().Any()
                    ? "[global::System.STAThread]"
                    : string.Empty;

            string GetAssemblyResolveHelperClass()
            {
                string impl = useShadowCopy
                    // used for LINQPad
                    ? $$"""
                    global::System.String guessedPath = global::System.IO.Path.Combine(@"{{shadowCopyFolderPath}}", $"{new global::System.Reflection.AssemblyName(args.Name).Name}.dll");
                                    return global::System.IO.File.Exists(guessedPath) ? global::System.Reflection.Assembly.LoadFrom(guessedPath) : null;
                    """
                    : """
                    global::System.Reflection.AssemblyName fullName = new global::System.Reflection.AssemblyName(args.Name);
                                    global::System.String simpleName = fullName.Name;

                                    global::System.String guessedPath = global::System.IO.Path.Combine(global::System.AppDomain.CurrentDomain.BaseDirectory, $"{simpleName}.dll");

                                    if (!global::System.IO.File.Exists(guessedPath))
                                    {
                                        global::System.Console.WriteLine($"// Wrong assembly binding redirects for {args.Name}.");
                                        return null; // we can't help, and we also don't call Assembly.Load which if fails comes back here, creates endless loop and causes StackOverflow
                                    }

                                    // the file is right there, but has most probably different version and there is no assembly binding redirect or there is a wrong one...
                                    // so we just load it and ignore the version mismatch

                                    // we warn the user about that, in case some Super User want to be aware of that
                                    global::System.Console.WriteLine($"// Wrong assembly binding redirects for {simpleName}, loading it from disk anyway.");

                                    return global::System.Reflection.Assembly.LoadFrom(guessedPath);
                    """;

                return $$"""
                private sealed class DirtyAssemblyResolveHelper : global::System.IDisposable
                        {
                            internal DirtyAssemblyResolveHelper() => global::System.AppDomain.CurrentDomain.AssemblyResolve += HelpTheFrameworkToResolveTheAssembly;

                            public void Dispose() => global::System.AppDomain.CurrentDomain.AssemblyResolve -= HelpTheFrameworkToResolveTheAssembly;

                            /// <summary>
                            /// according to https://msdn.microsoft.com/en-us/library/ff527268(v=vs.110).aspx
                            /// "the handler is invoked whenever the runtime fails to bind to an assembly by name."
                            /// </summary>
                            /// <returns>not null when we find it manually, null when we can't help</returns>
                            private global::System.Reflection.Assembly HelpTheFrameworkToResolveTheAssembly(global::System.Object sender, global::System.ResolveEventArgs args)
                            {
                                {{impl}}
                            }
                        }
                """;
            }
        }

        private static string GetBenchmarkRunCall(BuildPartition buildPartition, CodeGenBenchmarkRunCallType runCallType)
        {
            if (runCallType == CodeGenBenchmarkRunCallType.Reflection)
            {
                // Use reflection to call benchmark's Run method indirectly.
                return """
                await (global::System.Threading.Tasks.ValueTask) typeof(global::BenchmarkDotNet.Autogenerated.UniqueProgramName).Assembly
                                    .GetType($"BenchmarkDotNet.Autogenerated.Runnable_{id}")
                                    .GetMethod("Run", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static)
                                    .Invoke(null, new global::System.Object[] { host, benchmarkName, diagnoserRunMode });
                """;
            }

            // Generate a switch to call benchmark's Run method directly.
            var @switch = new StringBuilder(buildPartition.Benchmarks.Length * 30);
            @switch.AppendLine("switch (id) {");

            foreach (var buildInfo in buildPartition.Benchmarks)
            {
                @switch.AppendLine($"case {buildInfo.Id.Value}: await BenchmarkDotNet.Autogenerated.Runnable_{buildInfo.Id.Value}.Run(host, benchmarkName, diagnoserRunMode); break;");
            }

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
    }

    internal class SmartStringBuilder(string text)
    {
        private readonly StringBuilder builder = new(text);

        public SmartStringBuilder Replace(string oldValue, string? newValue)
        {
            if (text.Contains(oldValue))
                builder.Replace(oldValue, newValue);
            else
                builder.Append($"\n// '{oldValue}' not found");
            return this;
        }

        public override string ToString() => builder.ToString();
    }
}