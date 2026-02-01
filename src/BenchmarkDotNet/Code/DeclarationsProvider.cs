using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

#nullable enable

namespace BenchmarkDotNet.Code
{
    internal abstract class DeclarationsProvider(BenchmarkCase benchmark)
    {
        protected static readonly string CoreReturnType = typeof(ValueTask<ClockSpan>).GetCorrectCSharpTypeName();
        protected static readonly string CoreParameters = $"long invokeCount, {typeof(IClock).GetCorrectCSharpTypeName()} clock";
        protected static readonly string StartClockSyncCode = $"{typeof(StartedClock).GetCorrectCSharpTypeName()} startedClock = {typeof(ClockExtensions).GetCorrectCSharpTypeName()}.Start(clock);";
        protected static readonly string ReturnSyncCode = $"return new {CoreReturnType}(startedClock.GetElapsed());";
        private static readonly string ReturnCompletedValueTask = $"return new {typeof(ValueTask).GetCorrectCSharpTypeName()}();";

        protected BenchmarkCase Benchmark { get; } = benchmark;
        protected Descriptor Descriptor => Benchmark.Descriptor;

        public abstract string[] GetExtraFields();

        public SmartStringBuilder ReplaceTemplate(SmartStringBuilder smartStringBuilder)
        {
            Replace(smartStringBuilder, Descriptor.GlobalSetupMethod, "$GlobalSetupModifiers$", "$GlobalSetupImpl$", false);
            Replace(smartStringBuilder, Descriptor.GlobalCleanupMethod, "$GlobalCleanupModifiers$", "$GlobalCleanupImpl$", true);
            Replace(smartStringBuilder, Descriptor.IterationSetupMethod, "$IterationSetupModifiers$", "$IterationSetupImpl$", false);
            Replace(smartStringBuilder, Descriptor.IterationCleanupMethod, "$IterationCleanupModifiers$", "$IterationCleanupImpl$", false);
            return ReplaceCore(smartStringBuilder)
                .Replace("$DisassemblerEntryMethodImpl$", GetWorkloadMethodCall(GetPassArgumentsDirect()))
                .Replace("$OperationsPerInvoke$", Descriptor.OperationsPerInvoke.ToString())
                .Replace("$WorkloadTypeName$", Descriptor.Type.GetCorrectCSharpTypeName());
        }

        private void Replace(SmartStringBuilder smartStringBuilder, MethodInfo? method, string replaceModifiers, string replaceImpl, bool isGlobalCleanup)
        {
            string modifier;
            string impl;
            if (method == null)
            {
                modifier = string.Empty;
                impl = ReturnCompletedValueTask;
                if (isGlobalCleanup)
                {
                    impl = PrependExtraGlobalCleanupImpl(impl);
                }
                smartStringBuilder
                    .Replace(replaceModifiers, modifier)
                    .Replace(replaceImpl, impl);
                return;
            }

            if (method.ReturnType.IsAwaitable())
            {
                modifier = "async";
                impl = $"await {GetMethodPrefix(method)}.{method.Name}();";
            }
            else
            {
                modifier = string.Empty;
                impl = $"""
                {GetMethodPrefix(method)}.{method.Name}();
                            {ReturnCompletedValueTask}
                """;
            }
            if (isGlobalCleanup)
            {
                impl = PrependExtraGlobalCleanupImpl(impl);
            }
            smartStringBuilder
                .Replace(replaceModifiers, modifier)
                .Replace(replaceImpl, impl);
        }

        protected abstract string PrependExtraGlobalCleanupImpl(string impl);

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

    internal class SyncDeclarationsProvider(BenchmarkCase benchmark) : DeclarationsProvider(benchmark)
    {
        public override string[] GetExtraFields() => [];

        protected override string PrependExtraGlobalCleanupImpl(string impl) => impl;

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

    internal class AsyncDeclarationsProvider(BenchmarkCase benchmark) : DeclarationsProvider(benchmark)
    {
        public override string[] GetExtraFields() =>
        [
            $"public {typeof(WorkloadContinuerAndValueTaskSource).GetCorrectCSharpTypeName()} workloadContinuerAndValueTaskSource;",
            $"public {typeof(IClock).GetCorrectCSharpTypeName()} clock;",
            "public long invokeCount;"
        ];

        protected override string PrependExtraGlobalCleanupImpl(string impl)
            => $"""
            this.__fieldsContainer.workloadContinuerAndValueTaskSource?.Complete();
                        {impl}
            """;

        protected override SmartStringBuilder ReplaceCore(SmartStringBuilder smartStringBuilder)
        {
            // Unlike sync calls, async calls suffer from unrolling, so we multiply the invokeCount by the unroll factor and delegate the implementation to *NoUnroll methods.
            int unrollFactor = Benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance);
            string passArguments = GetPassArgumentsDirect();
            string workloadMethodCall = GetWorkloadMethodCall(passArguments);
            bool hasAsyncMethodBuilderAttribute = TryGetAsyncMethodBuilderAttribute(out var asyncMethodBuilderAttribute);
            Type workloadCoreReturnType = GetWorkloadCoreReturnType(hasAsyncMethodBuilderAttribute);
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
                        if (this.__fieldsContainer.workloadContinuerAndValueTaskSource == null)
                        {
                            this.__fieldsContainer.workloadContinuerAndValueTaskSource = new {{typeof(WorkloadContinuerAndValueTaskSource).GetCorrectCSharpTypeName()}}();
                            this.__StartWorkload();
                        }
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
                            while (true)
                            {
                                await this.__fieldsContainer.workloadContinuerAndValueTaskSource;
                                if (this.__fieldsContainer.workloadContinuerAndValueTaskSource.IsCompleted)
                                {
                                    {{finalReturn}}
                                }

                                {{typeof(StartedClock).GetCorrectCSharpTypeName()}} startedClock = {{typeof(ClockExtensions).GetCorrectCSharpTypeName()}}.Start(this.__fieldsContainer.clock);
                                while (--this.__fieldsContainer.invokeCount >= 0)
                                {
                                    // Necessary because of error CS4004: Cannot await in an unsafe context
                                    {{Descriptor.WorkloadMethod.ReturnType.GetCorrectCSharpTypeName()}} awaitable;
                                    unsafe { awaitable = {{workloadMethodCall}} }
                                    await awaitable;
                                }
                                this.__fieldsContainer.workloadContinuerAndValueTaskSource.SetResult(startedClock.GetElapsed());
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

        private bool TryGetAsyncMethodBuilderAttribute(out string asyncMethodBuilderAttribute)
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

        private Type GetWorkloadCoreReturnType(bool hasAsyncMethodBuilderAttribute)
        {
            if (Descriptor.WorkloadMethod.ResolveAttribute<AsyncCallerTypeAttribute>() is { } asyncCallerTypeAttribute)
            {
                return asyncCallerTypeAttribute.AsyncCallerType;
            }
            if (hasAsyncMethodBuilderAttribute
                || Descriptor.WorkloadMethod.ReturnType.HasAsyncMethodBuilderAttribute()
                // Task and Task<T> are not annotated with their builder type, the C# compiler special-cases them.
                || (Descriptor.WorkloadMethod.ReturnType.IsGenericType && Descriptor.WorkloadMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            )
            {
                return Descriptor.WorkloadMethod.ReturnType;
            }
            // Fallback to Task if the benchmark return type is Task or any awaitable type that is not a custom task-like type.
            return typeof(Task);
        }

        private static string GetFinalReturn(Type workloadCoreReturnType)
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
    }
}