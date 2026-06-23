using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using System.Reflection;
using System.Reflection.Emit;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;

partial class RunnableEmitter
{
    // Used when Job.Run.ConsumeTasksSynchronously is enabled for (Value)Task(<T>)-returning workloads.
    // Emits the same shape as SyncCoreEmitter but routes the workload return value through AwaitHelper.GetResult
    // so the iteration loop stays synchronous, matching the pre-async-refactor behavior so historical results stay comparable.
    private sealed class SyncTaskCoreEmitter(BuildPartition buildPartition, ModuleBuilder moduleBuilder, BenchmarkBuildInfo benchmark) : RunnableEmitter(buildPartition, moduleBuilder, benchmark)
    {
        // The workload is consumed synchronously, so the only async state machines are the setup/cleanup
        // methods. Without arguments there is no fields container declared before them, so their Roslyn
        // ordinals are two lower than in the async path (which always declares the fields container).
        // With arguments the fields container shifts them back to the async ordinals.
        protected override IReadOnlyDictionary<string, int> AsyncMethodToOrdinalMap
            => argFields.Count > 0
                ? base.AsyncMethodToOrdinalMap
                : new Dictionary<string, int>
                {
                    { GlobalSetupMethodName, 2 },
                    { GlobalCleanupMethodName, 3 },
                    { IterationSetupMethodName, 4 },
                    { IterationCleanupMethodName, 5 },
                };

        protected override void EmitExtraGlobalCleanup(ILGenerator ilBuilder, LocalBuilder? thisLocal) { }

        protected override void EmitCoreImpl()
        {
            EmitAction(OverheadActionUnrollMethodName, overheadImplementationMethod, jobUnrollFactor, isWorkload: false);
            EmitAction(OverheadActionNoUnrollMethodName, overheadImplementationMethod, 1, isWorkload: false);
            EmitAction(WorkloadActionUnrollMethodName, Descriptor.WorkloadMethod, jobUnrollFactor, isWorkload: true);
            EmitAction(WorkloadActionNoUnrollMethodName, Descriptor.WorkloadMethod, 1, isWorkload: true);
        }

        private MethodBuilder EmitAction(string methodName, MethodInfo methodToCall, int unrollFactor, bool isWorkload)
        {
            MethodInfo? getResultMethod = null;
            if (isWorkload)
            {
                getResultMethod = AwaitHelper.GetGetResultMethod(methodToCall.ReturnType)
                    ?? throw new InvalidOperationException(
                        $"AwaitHelper.GetResult is not available for workload return type {methodToCall.ReturnType.GetDisplayName()}. ConsumeTasksSynchronously only supports (Value)Task(<T>).");
            }

            var invokeCountArg = new EmitParameterInfo(0, InvokeCountParamName, typeof(long));
            var actionMethodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    methodName,
                    MethodAttributes.Private,
                    EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                    [
                        invokeCountArg,
                        new EmitParameterInfo(1, ClockParamName, typeof(IClock))
                    ]
                )
                .SetAggressiveOptimizationImplementationFlag();
            invokeCountArg.SetMember(actionMethodBuilder);

            var ilBuilder = actionMethodBuilder.GetILGenerator();

            var argLocals = argFields.Select(a => ilBuilder.DeclareLocal(a.ArgLocalsType)).ToList();
            var startedClockLocal = ilBuilder.DeclareLocal(typeof(StartedClock));

            // The workload is consumed synchronously via the blocking AwaitHelper.GetResult. BenchmarkDotNet never
            // installs a SynchronizationContext, so user awaits resume on the thread pool instead of being posted
            // back to this (blocked) thread.

            // load fields
            EmitLoadArgFieldsToLocals(ilBuilder, argLocals);

            // StartedClock startedClock = ClockExtensions.Start(clock);
            ilBuilder.Emit(OpCodes.Ldarg_2);
            ilBuilder.Emit(OpCodes.Call, GetStartClockMethod());
            ilBuilder.EmitStloc(startedClockLocal);

            // loop
            ilBuilder.EmitLoopBeginFromArgToZero(out var loopStartLabel, out var loopHeadLabel);
            {
                for (int u = 0; u < unrollFactor; u++)
                {
                    if (!methodToCall.IsStatic)
                    {
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                    }
                    ilBuilder.EmitLdLocals(argLocals);
                    ilBuilder.Emit(OpCodes.Call, methodToCall);

                    if (getResultMethod is not null)
                    {
                        // global::BenchmarkDotNet.Helpers.AwaitHelper.GetResult(<task on stack>);
                        ilBuilder.Emit(OpCodes.Call, getResultMethod);
                        // Generic Task<T>/ValueTask<T> overloads return T — discard it, mirroring the pre-refactor
                        // void ExecuteBlocking() => AwaitHelper.GetResult(callback()) implementation.
                        if (getResultMethod.ReturnType != typeof(void))
                        {
                            ilBuilder.Emit(OpCodes.Pop);
                        }
                    }
                    else if (methodToCall.ReturnType != typeof(void))
                    {
                        ilBuilder.Emit(OpCodes.Pop);
                    }
                }
            }
            ilBuilder.EmitLoopEndFromArgToZero(loopStartLabel, loopHeadLabel, invokeCountArg);

            // return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            ilBuilder.EmitLdloca(startedClockLocal);
            ilBuilder.Emit(OpCodes.Call, typeof(StartedClock).GetMethod(nameof(StartedClock.GetElapsed), BindingFlags.Public | BindingFlags.Instance)!);
            ilBuilder.Emit(OpCodes.Newobj, typeof(ValueTask<ClockSpan>).GetConstructor([typeof(ClockSpan)])!);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }

        private void EmitLoadArgFieldsToLocals(ILGenerator ilBuilder, List<LocalBuilder> argLocals)
        {
            for (int i = 0; i < argFields.Count; i++)
            {
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);

                var argFieldInfo = argFields[i];
                if (argFieldInfo.ArgLocalsType.IsByRef)
                    ilBuilder.Emit(OpCodes.Ldflda, argFieldInfo.Field);
                else
                    ilBuilder.Emit(OpCodes.Ldfld, argFieldInfo.Field);

                if (argFieldInfo.OpImplicitMethod != null)
                    ilBuilder.Emit(OpCodes.Call, argFieldInfo.OpImplicitMethod);

                ilBuilder.EmitStloc(argLocals[i]);
            }
        }
    }
}
