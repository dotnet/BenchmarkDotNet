using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Code
{
    internal abstract class DeclarationsProvider(BenchmarkCase benchmark)
    {
        protected static readonly string CoreReturnType = typeof(ValueTask<ClockSpan>).GetCorrectCSharpTypeName();
        protected static readonly string CoreParameters = $"long invokeCount, {typeof(IClock).GetCorrectCSharpTypeName()} clock";
        protected static readonly string StartClockSyncCode = $"{typeof(StartedClock).GetCorrectCSharpTypeName()} startedClock = {typeof(ClockExtensions).GetCorrectCSharpTypeName()}.Start(clock);";
        protected static readonly string ReturnSyncCode = $"return new {CoreReturnType}(startedClock.GetElapsed());";
        private static readonly string ReturnCompletedValueTask = $"return new {typeof(ValueTask).GetCorrectCSharpTypeName()}();";
        private enum ExtraImplKind { None, GlobalSetup, GlobalCleanup }

        protected BenchmarkCase Benchmark { get; } = benchmark;
        protected Descriptor Descriptor => Benchmark.Descriptor;

        public abstract string[] GetExtraFields();

        public SmartStringBuilder ReplaceTemplate(SmartStringBuilder smartStringBuilder)
        {
            Replace(smartStringBuilder, Descriptor.GlobalSetupMethod, "$GlobalSetupModifiers$", "$GlobalSetupImpl$", ExtraImplKind.GlobalSetup);
            Replace(smartStringBuilder, Descriptor.GlobalCleanupMethod, "$GlobalCleanupModifiers$", "$GlobalCleanupImpl$", ExtraImplKind.GlobalCleanup);
            Replace(smartStringBuilder, Descriptor.IterationSetupMethod, "$IterationSetupModifiers$", "$IterationSetupImpl$", ExtraImplKind.None);
            Replace(smartStringBuilder, Descriptor.IterationCleanupMethod, "$IterationCleanupModifiers$", "$IterationCleanupImpl$", ExtraImplKind.None);
            return ReplaceCore(smartStringBuilder)
                .Replace("$DisassemblerEntryMethodImpl$", GetWorkloadMethodCall(GetPassArgumentsDirect()))
                .Replace("$OperationsPerInvoke$", Descriptor.OperationsPerInvoke.ToString())
                .Replace("$WorkloadTypeName$", Descriptor.Type.GetCorrectCSharpTypeName());
        }

        private void Replace(SmartStringBuilder smartStringBuilder, MethodInfo? method, string replaceModifiers, string replaceImpl, ExtraImplKind extraImplKind)
        {
            string modifier;
            string userImpl;
            bool needsExplicitReturn;
            if (method == null)
            {
                modifier = string.Empty;
                userImpl = string.Empty;
                needsExplicitReturn = true;
            }
            else if (method.ReturnType.IsAwaitable(out _))
            {
                modifier = "async";
                userImpl = $"await {GetMethodPrefix(method)}.{method.Name}();";
                needsExplicitReturn = false;
            }
            else
            {
                modifier = string.Empty;
                userImpl = $"{GetMethodPrefix(method)}.{method.Name}();";
                needsExplicitReturn = true;
            }

            string explicitReturn = needsExplicitReturn ? ReturnCompletedValueTask : string.Empty;
            string impl = extraImplKind switch
            {
                // Append auto-generated setup code after user setup code.
                ExtraImplKind.GlobalSetup => CombineLines(userImpl, GetExtraGlobalSetupImpl(), explicitReturn),
                // Prepend auto-generated cleanup code before user cleanup code.
                ExtraImplKind.GlobalCleanup => CombineLines(GetExtraGlobalCleanupImpl(), userImpl, explicitReturn),
                _ => CombineLines(userImpl, explicitReturn),
            };

            smartStringBuilder
                .Replace(replaceModifiers, modifier)
                .Replace(replaceImpl, impl);
        }

        private static string CombineLines(params string[] parts)
            => string.Join($"{Environment.NewLine}            ", parts.Where(p => !string.IsNullOrEmpty(p)));

        protected virtual string GetExtraGlobalSetupImpl() => string.Empty;
        protected virtual string GetExtraGlobalCleanupImpl() => string.Empty;

        protected abstract SmartStringBuilder ReplaceCore(SmartStringBuilder smartStringBuilder);

        private static string GetMethodPrefix(MethodInfo method)
            => method.IsStatic ? method.DeclaringType!.GetCorrectCSharpTypeName() : "base";

        protected string GetWorkloadMethodCall(string passArguments)
             => $"{GetMethodPrefix(Descriptor.WorkloadMethod)}.{Descriptor.WorkloadMethod.Name}({passArguments});";

        protected string GetPassArgumentsDirect()
            => string.Join(
                ", ",
                Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) => $"{CodeGenerator.GetParameterModifier(parameter)} this.__fieldsContainer.argField{index}")
            );
    }

    internal sealed class SyncDeclarationsProvider(BenchmarkCase benchmark) : DeclarationsProvider(benchmark)
    {
        public override string[] GetExtraFields() => [];

        protected override SmartStringBuilder ReplaceCore(SmartStringBuilder smartStringBuilder)
        {
            string loadArguments = GetLoadArguments();
            string passArguments = GetPassArguments();
            string workloadMethodCall = GetWorkloadMethodCall(passArguments);
            string coreImpl = $$"""
            private unsafe {{CoreReturnType}} OverheadActionUnroll({{CoreParameters}})
                    {
                        {{loadArguments}}
                        {{StartClockSyncCode}}
                        while (--invokeCount >= 0)
                        {
                            this.__Overhead({{passArguments}});@Unroll@
                        }
                        {{ReturnSyncCode}}
                    }

                    private unsafe {{CoreReturnType}} OverheadActionNoUnroll({{CoreParameters}})
                    {
                        {{loadArguments}}
                        {{StartClockSyncCode}}
                        while (--invokeCount >= 0)
                        {
                            this.__Overhead({{passArguments}});
                        }
                        {{ReturnSyncCode}}
                    }

                    private unsafe {{CoreReturnType}} WorkloadActionUnroll({{CoreParameters}})
                    {
                        {{loadArguments}}
                        {{StartClockSyncCode}}
                        while (--invokeCount >= 0)
                        {
                            {{workloadMethodCall}}@Unroll@
                        }
                        {{ReturnSyncCode}}
                    }

                    private unsafe {{CoreReturnType}} WorkloadActionNoUnroll({{CoreParameters}})
                    {
                        {{loadArguments}}
                        {{StartClockSyncCode}}
                        while (--invokeCount >= 0)
                        {
                            {{workloadMethodCall}}
                        }
                        {{ReturnSyncCode}}
                    }
            """;

            return smartStringBuilder
                .Replace("$CoreImpl$", coreImpl);
        }

        private string GetLoadArguments()
            => string.Join(
                Environment.NewLine,
                Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) =>
                    {
                        var refModifier = parameter.ParameterType.IsByRef ? "ref" : string.Empty;
                        return $"{refModifier} {parameter.ParameterType.GetCorrectCSharpTypeName()} arg{index} = {refModifier} this.__fieldsContainer.argField{index};";
                    })
            );

        private string GetPassArguments()
            => string.Join(
                ", ",
                Descriptor.WorkloadMethod.GetParameters()
                    .Select((parameter, index) => $"{CodeGenerator.GetParameterModifier(parameter)} arg{index}")
            );
    }

    internal abstract class AsyncDeclarationsProviderBase(BenchmarkCase benchmark) : DeclarationsProvider(benchmark)
    {
        // Type used to drive the WorkloadCore builder selection. For ordinary awaitables it's the workload
        // method's own return type, but `IAsyncEnumerable<T>` has no GetAwaiter, so AsyncEnumerableDeclarationsProvider
        // overrides this to expose the MoveNextAsync awaitable as a proxy.
        protected virtual Type WorkloadAwaitableReturnType => Descriptor.WorkloadMethod.ReturnType;

        public override string[] GetExtraFields() =>
        [
            $"public {typeof(WorkloadValueTaskSource).GetCorrectCSharpTypeName()} workloadContinuerAndValueTaskSource;",
            $"public {typeof(IClock).GetCorrectCSharpTypeName()} clock;",
            "public long invokeCount;"
        ];

        protected override string GetExtraGlobalSetupImpl()
            => $$"""
            this.__fieldsContainer.workloadContinuerAndValueTaskSource = new {{typeof(WorkloadValueTaskSource).GetCorrectCSharpTypeName()}}();
                        this.__StartWorkload();
            """;

        protected override string GetExtraGlobalCleanupImpl()
            => "this.__fieldsContainer.workloadContinuerAndValueTaskSource.Complete();";

        protected bool TryGetAsyncMethodBuilderAttribute(out string asyncMethodBuilderAttribute)
        {
            asyncMethodBuilderAttribute = string.Empty;
            if (Descriptor.WorkloadMethod.HasAttribute<AsyncCallerTypeAttribute>())
            {
                return false;
            }
            if (Descriptor.WorkloadMethod.GetAsyncMethodBuilderAttribute() is not { } attr)
            {
                return false;
            }
            if (attr.GetType().GetProperty(nameof(AsyncMethodBuilderAttribute.BuilderType), BindingFlags.Public | BindingFlags.Instance)?.GetValue(attr) is not Type builderType)
            {
                return false;
            }
            asyncMethodBuilderAttribute = $"[{typeof(AsyncMethodBuilderAttribute).GetCorrectCSharpTypeName()}(typeof({builderType.GetCorrectCSharpTypeName()}))]";
            return true;
        }

        protected Type GetWorkloadCoreReturnType(bool hasAsyncMethodBuilderAttribute, Type returnType)
        {
            if (Descriptor.WorkloadMethod.ResolveAttribute<AsyncCallerTypeAttribute>() is { } asyncCallerTypeAttribute)
            {
                return asyncCallerTypeAttribute.AsyncCallerType;
            }
            if (hasAsyncMethodBuilderAttribute
                || returnType.HasAsyncMethodBuilderAttribute()
                // Task and Task<T> are not annotated with their builder type, the C# compiler special-cases them.
                || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            )
            {
                return returnType;
            }
            // Fallback to Task if the return type is Task or any awaitable type that is not a custom task-like type.
            return typeof(Task);
        }

        protected static string GetFinalReturn(Type workloadCoreReturnType)
        {
            var finalReturnType = workloadCoreReturnType
                .GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!
                .ReturnType!
                .GetMethod(nameof(TaskAwaiter.GetResult))!
                .ReturnType;
            return finalReturnType == typeof(void)
                ? "return;"
                : $"return default({finalReturnType.GetCorrectCSharpTypeName()});";
        }

        protected override SmartStringBuilder ReplaceCore(SmartStringBuilder smartStringBuilder)
        {
            // Unlike sync calls, async calls suffer from unrolling, so we multiply the invokeCount by the unroll factor and delegate the implementation to *NoUnroll methods.
            int unrollFactor = Benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance);
            string passArguments = GetPassArgumentsDirect();
            string workloadMethodCall = GetWorkloadMethodCall(passArguments);
            bool hasAsyncMethodBuilderAttribute = TryGetAsyncMethodBuilderAttribute(out var asyncMethodBuilderAttribute);
            Type workloadCoreReturnType = GetWorkloadCoreReturnType(hasAsyncMethodBuilderAttribute, WorkloadAwaitableReturnType);
            string finalReturn = GetFinalReturn(workloadCoreReturnType);
            string coreImpl = $$"""
            private {{CoreReturnType}} OverheadActionUnroll({{CoreParameters}})
                    {
                        return this.OverheadActionNoUnroll(invokeCount * {{unrollFactor}}, clock);
                    }

                    private {{CoreReturnType}} OverheadActionNoUnroll({{CoreParameters}})
                    {
                        {{StartClockSyncCode}}
                        while (--invokeCount >= 0)
                        {
                            this.__Overhead({{passArguments}});
                        }
                        {{ReturnSyncCode}}
                    }

                    private {{CoreReturnType}} WorkloadActionUnroll({{CoreParameters}})
                    {
                        return this.WorkloadActionNoUnroll(invokeCount * {{unrollFactor}}, clock);
                    }

                    private {{CoreReturnType}} WorkloadActionNoUnroll({{CoreParameters}})
                    {
                        this.__fieldsContainer.invokeCount = invokeCount;
                        this.__fieldsContainer.clock = clock;
                        // The source is allocated and the workload loop started in __GlobalSetup,
                        // so this hot path is branchless and allocation-free.
                        return this.__fieldsContainer.workloadContinuerAndValueTaskSource.Continue();
                    }

                    private async void __StartWorkload()
                    {
                        await __WorkloadCore();
                    }
            
                    {{asyncMethodBuilderAttribute}}
                    private async {{workloadCoreReturnType.GetCorrectCSharpTypeName()}} __WorkloadCore()
                    {
                        try
                        {
                            if (await this.__fieldsContainer.workloadContinuerAndValueTaskSource.GetIsComplete())
                            {
                                {{finalReturn}}
                            }
                            while (true)
                            {
                                {{typeof(StartedClock).GetCorrectCSharpTypeName()}} startedClock = {{typeof(ClockExtensions).GetCorrectCSharpTypeName()}}.Start(this.__fieldsContainer.clock);
                                while (--this.__fieldsContainer.invokeCount >= 0)
                                {
                                    {{GetCallAndConsumeImpl(workloadMethodCall)}}
                                }
                                if (await this.__fieldsContainer.workloadContinuerAndValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed()))
                                {
                                    {{finalReturn}}
                                }
                            }
                        }
                        catch (global::System.Exception e)
                        {
                            __fieldsContainer.workloadContinuerAndValueTaskSource.SetException(e);
                            {{finalReturn}}
                        }
                    }
            """;

            return smartStringBuilder
                .Replace("$CoreImpl$", coreImpl);
        }

        protected abstract string GetCallAndConsumeImpl(string workloadMethodCall);
    }

    internal class AsyncDeclarationsProvider(BenchmarkCase benchmark, Type resultType) : AsyncDeclarationsProviderBase(benchmark)
    {
        protected override string GetCallAndConsumeImpl(string workloadMethodCall)
        {
            string awaitStatement;
            if (resultType == typeof(void))
            {
                awaitStatement = "await awaitable;";
            }
            else
            {
                var resultTypeName = resultType.GetCorrectCSharpTypeName();
                awaitStatement = $"""
                {resultTypeName} result = await awaitable;
                                        {typeof(DeadCodeEliminationHelper).GetCorrectCSharpTypeName()}.KeepAliveWithoutBoxing<{resultTypeName}>(in result);
                """;
            }
            return $$"""
            // Necessary because of error CS4004: Cannot await in an unsafe context
                                    {{Descriptor.WorkloadMethod.ReturnType.GetCorrectCSharpTypeName()}} awaitable;
                                    unsafe { awaitable = {{workloadMethodCall}} }
                                    {{awaitStatement}}
            """;
        }
    }

    internal class AsyncEnumerableDeclarationsProvider(BenchmarkCase benchmark, Type itemType, Type moveNextAwaitableType) : AsyncDeclarationsProviderBase(benchmark)
    {
        protected override Type WorkloadAwaitableReturnType => moveNextAwaitableType;

        protected override string GetCallAndConsumeImpl(string workloadMethodCall)
        {
            string itemTypeName = itemType.GetCorrectCSharpTypeName();
            return $$"""
            // Necessary because of error CS4004: Cannot await in an unsafe context
                                    {{Descriptor.WorkloadMethod.ReturnType.GetCorrectCSharpTypeName()}} enumerable;
                                    unsafe { enumerable = {{workloadMethodCall}} }
                                    await foreach ({{itemTypeName}} item in enumerable)
                                    {
                                        {{typeof(DeadCodeEliminationHelper).GetCorrectCSharpTypeName()}}.KeepAliveWithoutBoxing<{{itemTypeName}}>(in item);
                                    }
            """;
        }
    }
}