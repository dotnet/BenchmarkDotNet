using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;

partial class RunnableEmitter
{
    private abstract class AsyncCoreEmitterBase(BuildPartition buildPartition, ModuleBuilder moduleBuilder, BenchmarkBuildInfo benchmark) : RunnableEmitter(buildPartition, moduleBuilder, benchmark)
    {
        protected FieldInfo workloadValueTaskSourceField = null!;
        protected FieldInfo clockField = null!;
        protected FieldInfo invokeCountField = null!;
        protected MethodInfo startWorkloadMethod = null!;

        protected override int GetExtraFieldsCount() => 3;

        protected override void EmitExtraFields(TypeBuilder fieldsContainerBuilder)
        {
            base.EmitExtraFields(fieldsContainerBuilder);

            workloadValueTaskSourceField = fieldsContainerBuilder.DefineField(
                WorkloadValueTaskSourceFieldName,
                typeof(WorkloadValueTaskSource),
                FieldAttributes.Public);
            clockField = fieldsContainerBuilder.DefineField(
                ClockFieldName,
                typeof(IClock),
                FieldAttributes.Public);
            invokeCountField = fieldsContainerBuilder.DefineField(
                InvokeCountFieldName,
                typeof(long),
                FieldAttributes.Public);
        }

        protected override void EmitExtraGlobalSetup(ILGenerator ilBuilder, LocalBuilder? thisLocal)
        {
            // __fieldsContainer.workloadValueTaskSource = new WorkloadValueTaskSource();
            EmitLoadThis(ilBuilder, thisLocal);
            ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
            ilBuilder.Emit(OpCodes.Newobj, typeof(WorkloadValueTaskSource).GetConstructor([])!);
            ilBuilder.Emit(OpCodes.Stfld, workloadValueTaskSourceField);

            // this.__StartWorkload();
            EmitLoadThis(ilBuilder, thisLocal);
            ilBuilder.Emit(OpCodes.Call, startWorkloadMethod);
        }

        protected override void EmitExtraGlobalCleanup(ILGenerator ilBuilder, LocalBuilder? thisLocal)
        {
            // __fieldsContainer.workloadValueTaskSource.Complete();
            EmitLoadThis(ilBuilder, thisLocal);
            ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
            ilBuilder.Emit(OpCodes.Ldfld, workloadValueTaskSourceField);
            ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.Complete), BindingFlags.Public | BindingFlags.Instance)!);
        }

        protected override void EmitCoreImpl()
        {
            EmitOverhead();
            EmitWorkloadCore();
            // After the derived class produces the async state machine and stashes startWorkloadMethod,
            // emit the WorkloadAction NoUnroll/Unroll stubs that the engine will call into.
            var noUnrollMethod = EmitWorkloadActionNoUnrollMethod();
            EmitWorkloadActionUnrollMethod(noUnrollMethod);
        }

        /// <summary>
        /// Derived classes implement this to emit the workload core state machine, the
        /// <c>__StartWorkload</c> async-void kicker, and assign <see cref="startWorkloadMethod"/>.
        /// </summary>
        protected abstract void EmitWorkloadCore();

        /// <summary>
        /// Resolves the <see cref="AsyncMethodBuilderAttribute.BuilderType"/> that should drive the
        /// <c>__WorkloadCore</c> state machine, mirroring the precedence the C# compiler uses:
        /// <c>[AsyncCallerType]</c> on the workload method wins; then a method-level
        /// <c>[AsyncMethodBuilder]</c>; then the same attribute on <paramref name="returnType"/>; then
        /// the <see cref="Task"/>/<see cref="Task{TResult}"/> special-case; otherwise
        /// <see cref="AsyncTaskMethodBuilder"/>. <paramref name="returnType"/> is the type whose builder
        /// should be inferred — for the awaitable path that's the workload's own return type, for the
        /// async-enumerable path it's a synthesized type derived from <c>MoveNextAsync</c>'s return type.
        /// </summary>
        protected Type GetWorkloadCoreAsyncMethodBuilderType(Type returnType)
        {
            // If the benchmark method overrode the caller type, use that type to get the builder type.
            if (Descriptor.WorkloadMethod.ResolveAttribute<AsyncCallerTypeAttribute>() is AsyncCallerTypeAttribute asyncCallerTypeAttribute)
            {
                return GetBuilderTypeFromUserSpecifiedAsyncType(asyncCallerTypeAttribute.AsyncCallerType);
            }
            // If the benchmark method overrode the builder, use the same builder.
            if (Descriptor.WorkloadMethod.GetAsyncMethodBuilderAttribute() is { } methodAttr
                && methodAttr.GetType().GetProperty(nameof(AsyncMethodBuilderAttribute.BuilderType), BindingFlags.Public | BindingFlags.Instance)?.GetValue(methodAttr) is Type methodBuilderType)
            {
                return methodBuilderType;
            }
            if (returnType.GetAsyncMethodBuilderAttribute() is { } typeAttr
                && typeAttr.GetType().GetProperty(nameof(AsyncMethodBuilderAttribute.BuilderType), BindingFlags.Public | BindingFlags.Instance)?.GetValue(typeAttr) is Type typeBuilderType)
            {
                return GetConcreteBuilderType(typeBuilderType, returnType);
            }
            // Task and Task<T> are not annotated with their builder type, the C# compiler special-cases them.
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return GetConcreteBuilderType(typeof(AsyncTaskMethodBuilder<>), returnType);
            }
            // Fallback to AsyncTaskMethodBuilder if the return type is Task or any awaitable type that is not a custom task-like type.
            return typeof(AsyncTaskMethodBuilder);

            Type GetBuilderTypeFromUserSpecifiedAsyncType(Type asyncType)
            {
                if (asyncType.GetAsyncMethodBuilderAttribute() is { } typeAttr
                    && typeAttr.GetType().GetProperty(nameof(AsyncMethodBuilderAttribute.BuilderType), BindingFlags.Public | BindingFlags.Instance)?.GetValue(typeAttr) is Type typeBuilderType)
                {
                    return GetConcreteBuilderType(typeBuilderType, asyncType);
                }
                // Task and Task<T> are not annotated with their builder type, the C# compiler special-cases them.
                if (asyncType == typeof(Task))
                {
                    return typeof(AsyncTaskMethodBuilder);
                }
                if (asyncType.IsGenericType && asyncType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    return typeof(AsyncTaskMethodBuilder<>).MakeGenericType([asyncType.GetGenericArguments()[0]]);
                }
                throw new NotSupportedException($"AsyncMethodBuilderAttribute not found on type {asyncType.GetDisplayName()} from {Descriptor.DisplayInfo}");
            }

            static Type GetConcreteBuilderType(Type builderType, Type forReturnType)
            {
                if (!builderType.IsGenericTypeDefinition)
                {
                    return builderType;
                }
                if (builderType.GetGenericArguments().Length != 1)
                {
                    throw new NotSupportedException($"AsyncMethodBuilder {builderType.GetDisplayName()} has generic arity greater than 1.");
                }
                var resultType = forReturnType
                    .GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!
                    .ReturnType
                    .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!
                    .ReturnType;
                return builderType.MakeGenericType([resultType]);
            }
        }

        private void EmitOverhead()
        {
            var noUnrollMethod = EmitOverheadNoUnrollMethod();
            EmitOverheadUnrollMethod(noUnrollMethod);
        }

        private MethodInfo EmitOverheadNoUnrollMethod()
        {
            /*
                // private ValueTask<ClockSpan> OverheadActionNoUnroll(long invokeCount, IClock clock)
                .method private hidebysig
                    instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan> OverheadActionNoUnroll (
                        int64 invokeCount,
                        class [Perfolizer]Perfolizer.Horology.IClock clock
                    ) cil managed flags(0200)
             */
            var invokeCountArg = new EmitParameterInfo(0, InvokeCountParamName, typeof(long));
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    OverheadActionNoUnrollMethodName,
                    MethodAttributes.Private,
                    EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                    [
                        invokeCountArg,
                        new EmitParameterInfo(1, ClockParamName, typeof(IClock))
                    ]
                )
                .SetAggressiveOptimizationImplementationFlag();
            invokeCountArg.SetMember(methodBuilder);

            var ilBuilder = methodBuilder.GetILGenerator();

            /*
                .locals init (
                    [0] valuetype [Perfolizer]Perfolizer.Horology.StartedClock startedClock
                )
             */
            var startedClockLocal = ilBuilder.DeclareLocal(typeof(StartedClock));

            /*
                // StartedClock startedClock = ClockExtensions.Start(clock);
                IL_0000: ldarg.2
                IL_0001: call valuetype [Perfolizer]Perfolizer.Horology.StartedClock [Perfolizer]Perfolizer.Horology.ClockExtensions::Start(class [Perfolizer]Perfolizer.Horology.IClock)
                IL_0006: stloc.0
             */
            ilBuilder.Emit(OpCodes.Ldarg_2);
            ilBuilder.Emit(OpCodes.Call, GetStartClockMethod());
            ilBuilder.EmitStloc(startedClockLocal);

            // loop
            ilBuilder.EmitLoopBeginFromArgToZero(out var loopStartLabel, out var loopHeadLabel);
            {
                /*
                    // __Overhead();
                    IL_0009: ldarg.0
                    IL_000a: call instance void BenchmarkDotNet.Autogenerated.Runnable_1::__Overhead()
                 */
                ilBuilder.Emit(OpCodes.Ldarg_0);
                EmitLoadArgFieldsForCall(ilBuilder, null);
                ilBuilder.Emit(OpCodes.Call, overheadImplementationMethod);
            }
            ilBuilder.EmitLoopEndFromArgToZero(loopStartLabel, loopHeadLabel, invokeCountArg);

            /*
                // return new ValueTask<ClockSpan>(startedClock.GetElapsed());
                IL_001a: ldloca.s 0
                IL_001c: call instance valuetype [Perfolizer]Perfolizer.Horology.ClockSpan [Perfolizer]Perfolizer.Horology.StartedClock::GetElapsed()
                IL_0021: newobj instance void valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan>::.ctor(!0)
                IL_0026: ret
             */
            ilBuilder.EmitLdloca(startedClockLocal);
            ilBuilder.Emit(OpCodes.Call, typeof(StartedClock).GetMethod(nameof(StartedClock.GetElapsed), BindingFlags.Public | BindingFlags.Instance)!);
            ilBuilder.Emit(OpCodes.Newobj, typeof(ValueTask<ClockSpan>).GetConstructor([typeof(ClockSpan)])!);
            ilBuilder.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private void EmitOverheadUnrollMethod(MethodInfo noUnrollMethod)
        {
            /*
                // private ValueTask<ClockSpan> OverheadActionUnroll(long invokeCount, IClock clock)
                .method private hidebysig
                    instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan> OverheadActionUnroll (
                        int64 invokeCount,
                        class [Perfolizer]Perfolizer.Horology.IClock clock
                    ) cil managed flags(0200)
             */
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    OverheadActionUnrollMethodName,
                    MethodAttributes.Private,
                    EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                    [
                        new EmitParameterInfo(0, InvokeCountParamName, typeof(long)),
                        new EmitParameterInfo(1, ClockParamName, typeof(IClock))
                    ]
                )
                .SetAggressiveOptimizationImplementationFlag();

            var ilBuilder = methodBuilder.GetILGenerator();

            /*
                // return OverheadActionNoUnroll(invokeCount * 16, clock);
                IL_0000: ldarg.0
                IL_0001: ldarg.1
                IL_0002: ldc.i4.s 16
                IL_0004: conv.i8
                IL_0005: mul
                IL_0006: ldarg.2
                IL_0007: call instance valuetype [System.Threading.Tasks.Extensions]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan> BenchmarkDotNet.Autogenerated.Runnable_0::OverheadActionNoUnroll(int64, class [Perfolizer]Perfolizer.Horology.IClock)
                IL_000c: ret
             */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldarg_1);
            ilBuilder.EmitLdc_I4(jobUnrollFactor);
            ilBuilder.Emit(OpCodes.Conv_I8);
            ilBuilder.Emit(OpCodes.Mul);
            ilBuilder.Emit(OpCodes.Ldarg_2);
            ilBuilder.Emit(OpCodes.Call, noUnrollMethod);
            ilBuilder.Emit(OpCodes.Ret);
        }

        private MethodInfo EmitWorkloadActionNoUnrollMethod()
        {
            /*
                // private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
                .method private hidebysig
                    instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan> WorkloadActionNoUnroll (
                        int64 invokeCount,
                        class [Perfolizer]Perfolizer.Horology.IClock clock
                    ) cil managed flags(0200)
             */
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    WorkloadActionNoUnrollMethodName,
                    MethodAttributes.Private,
                    EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                    [
                        new EmitParameterInfo(0, InvokeCountParamName, typeof(long)),
                        new EmitParameterInfo(1, ClockParamName, typeof(IClock))
                    ]
                )
                .SetAggressiveOptimizationImplementationFlag();

            var ilBuilder = methodBuilder.GetILGenerator();

            // __fieldsContainer.invokeCount = invokeCount;
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
            ilBuilder.Emit(OpCodes.Ldarg_1);
            ilBuilder.Emit(OpCodes.Stfld, invokeCountField);
            // __fieldsContainer.clock = clock;
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
            ilBuilder.Emit(OpCodes.Ldarg_2);
            ilBuilder.Emit(OpCodes.Stfld, clockField);
            // return __fieldsContainer.workloadValueTaskSource.Continue();
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
            ilBuilder.Emit(OpCodes.Ldfld, workloadValueTaskSourceField);
            ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.Continue), BindingFlags.Public | BindingFlags.Instance)!);
            ilBuilder.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private void EmitWorkloadActionUnrollMethod(MethodInfo noUnrollMethod)
        {
            /*
                // private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
                .method private hidebysig
                    instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan> WorkloadActionUnroll (
                        int64 invokeCount,
                        class [Perfolizer]Perfolizer.Horology.IClock clock
                    ) cil managed flags(0200)
             */
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    WorkloadActionUnrollMethodName,
                    MethodAttributes.Private,
                    EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                    [
                        new EmitParameterInfo(0, InvokeCountParamName, typeof(long)),
                        new EmitParameterInfo(1, ClockParamName, typeof(IClock))
                    ]
                )
                .SetAggressiveOptimizationImplementationFlag();

            var ilBuilder = methodBuilder.GetILGenerator();

            // return WorkloadActionNoUnroll(invokeCount * 16, clock);
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldarg_1);
            ilBuilder.EmitLdc_I4(jobUnrollFactor);
            ilBuilder.Emit(OpCodes.Conv_I8);
            ilBuilder.Emit(OpCodes.Mul);
            ilBuilder.Emit(OpCodes.Ldarg_2);
            ilBuilder.Emit(OpCodes.Call, noUnrollMethod);
            ilBuilder.Emit(OpCodes.Ret);
        }

        private static void EmitLoadThis(ILGenerator ilBuilder, LocalBuilder? thisLocal)
        {
            if (thisLocal != null)
                ilBuilder.EmitLdloc(thisLocal);
            else
                ilBuilder.Emit(OpCodes.Ldarg_0);
        }
    }
}
