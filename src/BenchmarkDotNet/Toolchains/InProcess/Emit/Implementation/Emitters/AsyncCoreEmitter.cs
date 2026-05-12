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
    // TODO: update this to support runtime-async.
    private sealed class AsyncCoreEmitter(BuildPartition buildPartition, ModuleBuilder moduleBuilder, BenchmarkBuildInfo benchmark, AwaitableInfo awaitableInfo) : AsyncCoreEmitterBase(buildPartition, moduleBuilder, benchmark)
    {
        protected override void EmitWorkloadCore()
        {
            var asyncMethodBuilderType = GetWorkloadCoreAsyncMethodBuilderType(Descriptor.WorkloadMethod.ReturnType);
            var builderInfo = BeginAsyncStateMachineTypeBuilder(WorkloadCoreMethodName, asyncMethodBuilderType, runnableBuilder);
            var (asyncStateMachineTypeBuilder, publicFields, (ilBuilder, endTryLabel, returnLabel, stateLocal, thisLocal, returnDefaultLocal)) = builderInfo;
            var (stateField, builderField, _) = publicFields;
            var startedClockField = asyncStateMachineTypeBuilder.DefineField(
                "<startedClock>5__2",
                typeof(StartedClock),
                FieldAttributes.Private
            );
            var workloadContinuerAwaiterField = asyncStateMachineTypeBuilder.DefineField(
                "<>u__1",
                typeof(ValueTaskAwaiter<bool>),
                FieldAttributes.Private
            );
            var benchmarkAwaiterField = asyncStateMachineTypeBuilder.DefineField(
                "<>u__2",
                awaitableInfo.AwaiterType,
                FieldAttributes.Private
            );
            EmitMoveNextImpl();
            var asyncStateMachineType = CompleteAsyncStateMachineType(asyncMethodBuilderType, builderInfo);

            var workloadCoreMethod = EmitAsyncCallerStub(WorkloadCoreMethodName, asyncStateMachineType, publicFields);

            startWorkloadMethod = EmitAsyncSingleCall(StartWorkloadMethodName, typeof(AsyncVoidMethodBuilder), workloadCoreMethod, SetupCleanupKind.Other);

            void EmitMoveNextImpl()
            {
                var resultType = awaitableInfo.ResultType;
                var isCompleteAwaiterLocal = ilBuilder.DeclareLocal(typeof(ValueTaskAwaiter<bool>));
                var isCompleteAwaitableLocal = ilBuilder.DeclareLocal(typeof(ValueTask<bool>));
                // The value-type awaitable spill local (Roslyn declares one for ValueTask<T> et al. so it
                // can take its address for GetAwaiter — reference-type awaitables stay on the stack).
                var benchmarkAwaitableLocal = Descriptor.WorkloadMethod.ReturnType.IsValueType
                    ? ilBuilder.DeclareLocal(Descriptor.WorkloadMethod.ReturnType)
                    : null;
                // Source local for `T result = await awaitable;` — declared only when the awaiter's
                // GetResult returns non-void, in which case the template captures the value and pipes
                // it through DeadCodeEliminationHelper so the JIT can't elide the producer's work.
                // Roslyn places this AFTER the (optional) awaitable spill and BEFORE the awaiter temp.
                var resultLocal = resultType == typeof(void)
                    ? null
                    : ilBuilder.DeclareLocal(resultType);
                var benchmarkAwaiterLocal = ilBuilder.DeclareLocal(benchmarkAwaiterField.FieldType);
                var invokeCountLocal = ilBuilder.DeclareLocal(typeof(long));
                var exceptionLocal = ilBuilder.DeclareLocal(typeof(Exception));

                var getIsCompleteContinuationLabel = ilBuilder.DefineLabel();
                var getIsCompleteGetResultLabel = ilBuilder.DefineLabel();
                var startClockLabel = ilBuilder.DefineLabel();
                var callBenchmarkLabel = ilBuilder.DefineLabel();
                var callBenchmarkLoopLabel = ilBuilder.DefineLabel();
                var benchmarkContinuationLabel = ilBuilder.DefineLabel();
                var benchmarkContinuationGetResultLabel = ilBuilder.DefineLabel();
                var setResultContinuationLabel = ilBuilder.DefineLabel();
                var setResultGetResultLabel = ilBuilder.DefineLabel();

                // Roslyn preamble — the C# compiler emits this dead-code sequence at the start of async
                // state machine MoveNext methods. Keeping it makes the EmitsSameIL diff line up.
                ilBuilder.EmitLdloc(stateLocal);
                ilBuilder.Emit(OpCodes.Ldc_I4_2);
                ilBuilder.Emit(OpCodes.Pop);
                ilBuilder.Emit(OpCodes.Pop);
                ilBuilder.Emit(OpCodes.Nop);

                ilBuilder.BeginExceptionBlock();
                {
                    // State dispatch: 0 → getIsComplete resume, 1 → benchmark resume, 2 → setResult resume.
                    ilBuilder.EmitLdloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Switch, [getIsCompleteContinuationLabel, benchmarkContinuationLabel, setResultContinuationLabel]);

                    // ===== await workloadValueTaskSource.GetIsComplete() =====
                    // var awaitable = workloadValueTaskSource.GetIsComplete();
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAndValueTaskSourceField);
                    ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.GetIsComplete), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaitableLocal);
                    // var awaiter = awaitable.GetAwaiter();
                    ilBuilder.EmitLdloca(isCompleteAwaitableLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTask<bool>).GetMethod(nameof(ValueTask<bool>.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaiterLocal);
                    // if (awaiter.IsCompleted) goto getIsCompleteGetResultLabel;
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetProperty(nameof(ValueTaskAwaiter<bool>.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!);
                    ilBuilder.Emit(OpCodes.Brtrue, getIsCompleteGetResultLabel);
                    // state = 0; <>u__1 = awaiter;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_0);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Stfld, workloadContinuerAwaiterField);
                    // <>t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, builderField);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, typeof(ValueTaskAwaiter<bool>), asyncStateMachineTypeBuilder));
                    // return;
                    ilBuilder.Emit(OpCodes.Leave, returnLabel);

                    // --- Resume from state 0 ---
                    ilBuilder.MarkLabel(getIsCompleteContinuationLabel);
                    // awaiter = <>u__1;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAwaiterField);
                    ilBuilder.EmitStloc(isCompleteAwaiterLocal);
                    // <>u__1 = default;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(workloadContinuerAwaiterField);
                    // state = -1;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_M1);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);

                    // --- GetResult ---
                    ilBuilder.MarkLabel(getIsCompleteGetResultLabel);
                    // bool isComplete = awaiter.GetResult();
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetMethod(nameof(ValueTaskAwaiter<bool>.GetResult), BindingFlags.Public | BindingFlags.Instance)!);
                    // if (!isComplete) goto startClockLabel;
                    ilBuilder.Emit(OpCodes.Brfalse, startClockLabel);
                    // return default;
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);
                    ilBuilder.Emit(OpCodes.Leave, endTryLabel);

                    // ===== while(true) { startClock → benchmark loop → SetResultAndGetIsComplete } =====

                    // startedClock = ClockExtensions.Start(clock);
                    ilBuilder.MarkLabel(startClockLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, clockField);
                    ilBuilder.Emit(OpCodes.Call, GetStartClockMethod());
                    ilBuilder.Emit(OpCodes.Stfld, startedClockField);
                    // goto callBenchmarkLoopLabel;
                    ilBuilder.Emit(OpCodes.Br, callBenchmarkLoopLabel);

                    // --- Benchmark call ---
                    ilBuilder.MarkLabel(callBenchmarkLabel);
                    if (!Descriptor.WorkloadMethod.IsStatic)
                    {
                        ilBuilder.EmitLdloc(thisLocal!);
                    }
                    EmitLoadArgFieldsForCall(ilBuilder, thisLocal);
                    ilBuilder.Emit(OpCodes.Call, Descriptor.WorkloadMethod);
                    if (benchmarkAwaitableLocal == null)
                    {
                        ilBuilder.Emit(OpCodes.Callvirt, awaitableInfo.GetAwaiterMethod);
                    }
                    else
                    {
                        ilBuilder.EmitStloc(benchmarkAwaitableLocal);
                        ilBuilder.EmitLdloca(benchmarkAwaitableLocal);
                        ilBuilder.Emit(OpCodes.Call, awaitableInfo.GetAwaiterMethod);
                    }
                    // if (awaiter.IsCompleted) goto benchmarkContinuationGetResultLabel;
                    ilBuilder.EmitStloc(benchmarkAwaiterLocal);
                    ilBuilder.EmitLdloca(benchmarkAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, awaitableInfo.IsCompletedProperty.GetMethod!);
                    ilBuilder.Emit(OpCodes.Brtrue, benchmarkContinuationGetResultLabel);
                    // state = 1; <>u__2 = awaiter;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_1);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(benchmarkAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Stfld, benchmarkAwaiterField);
                    // <>t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, builderField);
                    ilBuilder.EmitLdloca(benchmarkAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, benchmarkAwaiterField.FieldType, asyncStateMachineTypeBuilder));
                    // return;
                    ilBuilder.Emit(OpCodes.Leave, returnLabel);

                    // --- Resume from state 1 ---
                    ilBuilder.MarkLabel(benchmarkContinuationLabel);
                    // awaiter = <>u__2;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, benchmarkAwaiterField);
                    ilBuilder.EmitStloc(benchmarkAwaiterLocal);
                    // <>u__2 = default;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(benchmarkAwaiterField);
                    // state = -1;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_M1);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);

                    // --- Benchmark GetResult ---
                    ilBuilder.MarkLabel(benchmarkContinuationGetResultLabel);
                    ilBuilder.EmitLdloca(benchmarkAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, awaitableInfo.GetResultMethod);
                    if (resultLocal is not null)
                    {
                        // Mirror the template's `T result = await awaitable; KeepAliveWithoutBoxing(in result);`:
                        // store the GetResult value into a local and pass it to the non-inlined sink so the
                        // JIT can't elide whatever produced it.
                        ilBuilder.EmitStloc(resultLocal);
                        ilBuilder.EmitLdloca(resultLocal);
                        var keepAliveInMethod = typeof(DeadCodeEliminationHelper)
                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .First(m => m.Name == nameof(DeadCodeEliminationHelper.KeepAliveWithoutBoxing)
                                && m.GetParameters().Length == 1
                                && m.GetParameters()[0].ParameterType.IsByRef)
                            .MakeGenericMethod(resultType);
                        ilBuilder.Emit(OpCodes.Call, keepAliveInMethod);
                    }

                    // --- Benchmark loop: if (--invokeCount >= 0) goto callBenchmarkLabel ---
                    ilBuilder.MarkLabel(callBenchmarkLoopLabel);
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldflda, invokeCountField);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.Emit(OpCodes.Ldind_I8);
                    ilBuilder.Emit(OpCodes.Ldc_I4_1);
                    ilBuilder.Emit(OpCodes.Conv_I8);
                    ilBuilder.Emit(OpCodes.Sub);
                    ilBuilder.EmitStloc(invokeCountLocal);
                    ilBuilder.EmitLdloc(invokeCountLocal);
                    ilBuilder.Emit(OpCodes.Stind_I8);
                    ilBuilder.EmitLdloc(invokeCountLocal);
                    ilBuilder.Emit(OpCodes.Ldc_I4_0);
                    ilBuilder.Emit(OpCodes.Conv_I8);
                    ilBuilder.Emit(OpCodes.Bge, callBenchmarkLabel);

                    // ===== await workloadValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed()) =====
                    // var awaitable = workloadValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed());
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAndValueTaskSourceField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
                    ilBuilder.Emit(OpCodes.Call, typeof(StartedClock).GetMethod(nameof(StartedClock.GetElapsed), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.SetResultAndGetIsComplete), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaitableLocal);
                    // var awaiter = awaitable.GetAwaiter();
                    ilBuilder.EmitLdloca(isCompleteAwaitableLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTask<bool>).GetMethod(nameof(ValueTask<bool>.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaiterLocal);
                    // if (awaiter.IsCompleted) goto setResultGetResultLabel;
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetProperty(nameof(ValueTaskAwaiter<bool>.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!);
                    ilBuilder.Emit(OpCodes.Brtrue, setResultGetResultLabel);
                    // state = 2; <>u__1 = awaiter;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_2);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Stfld, workloadContinuerAwaiterField);
                    // <>t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, builderField);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, typeof(ValueTaskAwaiter<bool>), asyncStateMachineTypeBuilder));
                    // return;
                    ilBuilder.Emit(OpCodes.Leave, returnLabel);

                    // --- Resume from state 2 ---
                    ilBuilder.MarkLabel(setResultContinuationLabel);
                    // awaiter = <>u__1;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAwaiterField);
                    ilBuilder.EmitStloc(isCompleteAwaiterLocal);
                    // <>u__1 = default;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(workloadContinuerAwaiterField);
                    // state = -1;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_M1);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);

                    // --- SetResult GetResult ---
                    ilBuilder.MarkLabel(setResultGetResultLabel);
                    // bool isComplete = awaiter.GetResult();
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetMethod(nameof(ValueTaskAwaiter<bool>.GetResult), BindingFlags.Public | BindingFlags.Instance)!);
                    // if (!isComplete) goto continueLoopLabel;
                    var continueLoopLabel = ilBuilder.DefineLabel();
                    ilBuilder.Emit(OpCodes.Brfalse, continueLoopLabel);
                    // return default;
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);
                    ilBuilder.Emit(OpCodes.Leave, endTryLabel);
                    // continueLoopLabel: startedClock = default; goto startClockLabel;
                    ilBuilder.MarkLabel(continueLoopLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(startedClockField);
                    ilBuilder.Emit(OpCodes.Br, startClockLabel);
                }
                // end .try
                ilBuilder.BeginCatchBlock(typeof(Exception));
                {
                    // catch (Exception exception)
                    ilBuilder.EmitStloc(exceptionLocal);
                    // workloadValueTaskSource.SetException(exception);
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAndValueTaskSourceField);
                    ilBuilder.EmitLdloc(exceptionLocal);
                    ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.SetException), [typeof(Exception)])!);
                    // result = default;
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);
                    // return;
                    ilBuilder.Emit(OpCodes.Leave, endTryLabel);

                    ilBuilder.EndExceptionBlock();
                } // end handler
            }
        }
    }
}
