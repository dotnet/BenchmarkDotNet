using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using RunMode = BenchmarkDotNet.Jobs.RunMode;

namespace BenchmarkDotNet.Code
{
    internal static class CodeGenerator
    {
        internal static async ValueTask<string> GenerateAsync(BuildPartition buildPartition, CodeGenEntryPointType entryPointType, CodeGenBenchmarkRunCallType benchmarkRunCallType, CancellationToken cancellationToken)
        {
            (bool useShadowCopy, string shadowCopyFolderPath) = GetShadowCopySettings();

            var benchmarksCode = new List<string>(buildPartition.Benchmarks.Length);

            string benchmarkTypeTemplate = await ResourceHelper.LoadTemplateAsync("BenchmarkType.txt", cancellationToken).ConfigureAwait(false);
            foreach (var buildInfo in buildPartition.Benchmarks)
            {
                var benchmark = buildInfo.BenchmarkCase;

                var declarationsProvider = GetDeclarationsProvider(benchmark);
                var extraFields = declarationsProvider.GetExtraFields();
                var parameterRenderer = CSharpParameterRenderer.Create(benchmark);

                string benchmarkTypeCode = declarationsProvider
                    .ReplaceTemplate(new SmartStringBuilder(benchmarkTypeTemplate))
                    .Replace("$ID$", buildInfo.Id.ToString())
                    .Replace("$JobSetDefinition$", GetJobsSetDefinition(benchmark))
                    .Replace("$SourceOutParameters$", parameterRenderer.RenderSourceOutParameters())
                    .Replace("$SourceOutArguments$", parameterRenderer.RenderSourceOutParameters())
                    .Replace("$SourceCaptures$", parameterRenderer.RenderSourceCaptures())
                    .Replace("$ParamsInitializer$", GetParamsInitializer(benchmark, parameterRenderer))
                    .Replace("$CancellationTokenAssignment$", GetCancellationTokenAssignment(benchmark))
                    .Replace("$CancellationTokenInitializer$", GetCancellationTokenInitializer(benchmark))
                    .Replace("$ArgumentsDefinition$", GetArgumentsDefinition(benchmark))
                    .Replace("$DeclareFieldsContainer$", GetDeclareFieldsContainer(benchmark, buildInfo.Id, extraFields))
                    .Replace("$StaticParamsAndArgsContent$", GetStaticParamsAndArgsContent(benchmark, parameterRenderer))
                    .Replace("$EngineFactoryType$", GetEngineFactoryTypeName(benchmark))
                    .Replace("$RunExtraIteration$", buildInfo.Config.HasExtraIterationDiagnoser(benchmark) ? "true" : "false")
                    .Replace("$DisassemblerEntryMethodName$", RunnableConstants.ForDisassemblyDiagnoserMethodName)
                    .Replace("$InProcessDiagnoserRouters$", GetInProcessDiagnoserRouters(buildInfo))
                    .ToString();

                benchmarkTypeCode = Unroll(benchmarkTypeCode, benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance));

                benchmarksCode.Add(benchmarkTypeCode);
            }

            string benchmarkProgramContent = new SmartStringBuilder(await ResourceHelper.LoadTemplateAsync("BenchmarkProgram.txt", cancellationToken).ConfigureAwait(false))
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

            if (method.ReturnType.IsAwaitable(out var awaitableInfo))
            {
                if (benchmark.Job.ResolveValue(RunMode.ConsumeTasksSynchronouslyCharacteristic, EnvironmentResolver.Instance)
                    && AwaitHelper.IsBuiltInTaskType(method.ReturnType))
                {
                    return new SyncTaskDeclarationsProvider(benchmark);
                }
                return new AsyncDeclarationsProvider(benchmark, awaitableInfo.ResultType);
            }

            if (method.ReturnType.IsAsyncEnumerable(out var asyncEnumerableInfo))
            {
                return new AsyncEnumerableDeclarationsProvider(benchmark, asyncEnumerableInfo.ItemType, asyncEnumerableInfo.MoveNextAsyncMethod.ReturnType);
            }

            if (method.ReturnType == typeof(void) && method.HasAttribute<AsyncStateMachineAttribute>())
            {
                throw new NotSupportedException("async void is not supported by design");
            }

            return new SyncDeclarationsProvider(benchmark);
        }

        // internal for tests
        internal static string GetParamsInitializer(BenchmarkCase benchmarkCase)
            => GetParamsInitializer(benchmarkCase, CSharpParameterRenderer.Create(benchmarkCase));

        private static string GetParamsInitializer(BenchmarkCase benchmarkCase, CSharpParameterRenderer renderer)
            => string.Join(
                $",{Environment.NewLine}                ",
                benchmarkCase.Parameters.Items
                    .Where(parameter => !parameter.IsArgument && !parameter.IsStatic)
                    .Select(parameter => $"{parameter.Name} = {renderer.Render(parameter.ParameterValue)}"));

        // Static [BenchmarkCancellation] members only - instance members are set by the object initializer
        // (GetCancellationTokenInitializer), so emitting them here too would assign them twice.
        internal static string GetCancellationTokenAssignment(BenchmarkCase benchmarkCase)
        {
            var targetType = benchmarkCase.Descriptor.Type;
            List<string> cancellationTokenMembers = [];
            var typeFullName = targetType.GetCorrectCSharpTypeName();

            // As in GetCancellationTokenInitializer: one entry per name. Here a repeat compiles, since these are
            // statements, but `Type.Name` binds to the most derived member both times - so it would assign that
            // one twice and the member hiding it never.
            HashSet<string> emitted = new(StringComparer.Ordinal);

            // FlattenHierarchy reaches a base type's statics, which reflection otherwise withholds - the same set
            // BenchmarkCancellationValidator reports on, so it cannot accept a member this never assigns.
            // Check properties
            foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (property.PropertyType == typeof(CancellationToken) &&
                    property.IsDefined(typeof(Attributes.BenchmarkCancellationAttribute), inherit: false) &&
                    property.CanWrite &&
                    property.GetSetMethod() is { IsStatic: true } &&
                    emitted.Add(property.Name))
                {
                    cancellationTokenMembers.Add($"            {typeFullName}.{property.Name} = cancellationToken;");
                }
            }

            // Check fields
            foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (field.FieldType == typeof(CancellationToken) &&
                    field.IsDefined(typeof(Attributes.BenchmarkCancellationAttribute), inherit: false) &&
                    emitted.Add(field.Name))
                {
                    cancellationTokenMembers.Add($"            {typeFullName}.{field.Name} = cancellationToken;");
                }
            }

            return cancellationTokenMembers.Count > 0
                ? string.Join(Environment.NewLine, cancellationTokenMembers) + Environment.NewLine
                : string.Empty;
        }

        private static string GetCancellationTokenInitializer(BenchmarkCase benchmarkCase)
        {
            var targetType = benchmarkCase.Descriptor.Type;
            List<string> entries = [];

            // One entry per name. GetFields hands back a hidden base field alongside the `new` one that hides it -
            // GetProperties does not, which is why only fields reach this - and the same name twice in an object
            // initializer is CS1912. The name binds to the most derived member either way, so the second entry
            // could only ever repeat the first.
            HashSet<string> emitted = new(StringComparer.Ordinal);

            foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType == typeof(CancellationToken) &&
                    property.IsDefined(typeof(Attributes.BenchmarkCancellationAttribute), inherit: false) &&
                    property.CanWrite &&
                    property.GetSetMethod() is { IsStatic: false } &&
                    emitted.Add(property.Name))
                {
                    entries.Add($"{property.Name} = cancellationToken,");
                }
            }

            foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(CancellationToken) &&
                    field.IsDefined(typeof(Attributes.BenchmarkCancellationAttribute), inherit: false) &&
                    !field.IsStatic &&
                    emitted.Add(field.Name))
                {
                    entries.Add($"{field.Name} = cancellationToken,");
                }
            }

            return entries.Count > 0
                ? string.Join($"{Environment.NewLine}                ", entries)
                : string.Empty;
        }

        private static string GetArgumentsDefinition(BenchmarkCase benchmarkCase)
            => string.Join(
                ", ",
                benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) => $"{GetParameterModifier(parameter)} {parameter.ParameterType.GetCorrectCSharpTypeName()} {RunnableConstants.ArgParamPrefix}{index}"));

        private static string GetDeclareFieldsContainer(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, string[] extraFields)
        {
            var fields = benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                .Select((parameter, index) => $"public {GetFieldType(parameter.ParameterType, benchmarkCase.Parameters.GetArgument(parameter.Name!)).GetCorrectCSharpTypeName()} {RunnableConstants.ArgFieldPrefix}{index};")
                .Concat(extraFields)
                .ToArray();

            // Prevent CS0169
            if (fields.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine($$"""
                    [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
                    private struct {{RunnableConstants.FieldsContainerTypeName}}
                    {
            """);
            foreach (var field in fields)
            {
                sb.AppendLine($"            {field}");
            }
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine($"        private global::{RunnableConstants.EmittedTypePrefix}{benchmarkId.Value}.{RunnableConstants.FieldsContainerTypeName} {RunnableConstants.FieldsContainerName};");
            return sb.ToString();
        }

        /*

        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        private unsafe struct __FieldsContainer
        {
            $DeclareArgumentFields$
            $ExtraFields$
        }

        private global::BenchmarkDotNet.Autogenerated.Runnable_$ID$.__FieldsContainer __fieldsContainer;

         */

        // Assigned after the instance is created: argument fields live on it, and a static parameter may draw its
        // value from an instance source, which the constructor captures.
        private static string GetStaticParamsAndArgsContent(BenchmarkCase benchmarkCase, CSharpParameterRenderer renderer)
        {
            var staticParams = benchmarkCase.Parameters.Items
                .Where(parameter => !parameter.IsArgument && parameter.IsStatic)
                .Select(parameter => $"{benchmarkCase.Descriptor.Type.GetCorrectCSharpTypeName()}.{parameter.Name} = {renderer.Render(parameter.ParameterValue)};");

            var argumentFields = benchmarkCase.Descriptor.WorkloadMethod.GetParameters()
                .Select((parameter, index) => $"instance.{RunnableConstants.FieldsContainerName}.{RunnableConstants.ArgFieldPrefix}{index} = {renderer.Render(benchmarkCase.Parameters.GetArgument(parameter.Name!).ParameterValue)};");

            return string.Join(
                $"{Environment.NewLine}            ",
                renderer.RenderStatementReads().Concat(staticParams).Concat(argumentFields));
        }

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
            var compositeInProcessDiagnoser = buildInfo.CompositeInProcessDiagnoser;
            var handlerData = compositeInProcessDiagnoser.GetHandlerData(buildInfo.BenchmarkCase);
            var sourceCodes = compositeInProcessDiagnoser.InProcessDiagnosers
                .Select((diagnoser, index) => ToSourceCode(diagnoser, handlerData[index], buildInfo.BenchmarkCase, index))
                .WhereNotNull();
            return string.Join($",\n", sourceCodes);

            static string? ToSourceCode(IInProcessDiagnoser diagnoser, InProcessDiagnoserHandlerData handlerData, BenchmarkCase benchmarkCase, int index)
            {
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
                return $$"""
                await ((global::System.Threading.Tasks.ValueTask) typeof(global::BenchmarkDotNet.Autogenerated.UniqueProgramName).Assembly
                                    .GetType($"{{RunnableConstants.EmittedTypePrefix}}{id}")
                                    .GetMethod("{{RunnableConstants.RunMethodName}}", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static)
                                    .Invoke(null, new global::System.Object[] { host, benchmarkName, diagnoserRunMode }))
                                    .ConfigureAwait(false);
                """;
            }

            // Generate a switch to call benchmark's Run method directly.
            var @switch = new StringBuilder(buildPartition.Benchmarks.Length * 30);
            @switch.AppendLine("switch (id) {");

            foreach (var buildInfo in buildPartition.Benchmarks)
            {
                @switch.AppendLine($"case {buildInfo.Id.Value}: await {RunnableConstants.EmittedTypePrefix}{buildInfo.Id.Value}.{RunnableConstants.RunMethodName}(host, benchmarkName, diagnoserRunMode); break;");
            }

            @switch.AppendLine("default: throw new System.NotSupportedException(\"invalid benchmark id\");");
            @switch.AppendLine("}");

            return @switch.ToString();
        }

        private static Type GetFieldType(Type argumentType, ParameterInstance argument)
        {
            // #774 we can't store ByRefLike in a field, so we store what the value is cast to (which is later converted back to the ByRefLike when we load the arguments).
            if (argumentType.WithoutRefModifier().IsByRefLike() && argument.Value is { } value)
                return value.GetType();

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