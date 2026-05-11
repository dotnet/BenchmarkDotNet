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
    private sealed class AsyncEnumerableCoreEmitter : AsyncCoreEmitterBase
    {
        private readonly Type itemType;
        private readonly EnumerableInfo enumerableInfo;

        public AsyncEnumerableCoreEmitter(BuildPartition buildPartition, ModuleBuilder moduleBuilder, BenchmarkBuildInfo benchmark)
            : base(buildPartition, moduleBuilder, benchmark)
        {
            var workloadReturnType = benchmark.BenchmarkCase.Descriptor.WorkloadMethod.ReturnType;
            enumerableInfo = ResolveEnumerableInfo(workloadReturnType);
            itemType = enumerableInfo.ItemType;
        }

        protected override void EmitWorkloadCore()
        {
            var asyncMethodBuilderType = GetWorkloadCoreAsyncMethodBuilderType(enumerableInfo.MoveNextAsyncMethod.ReturnType);
            var builderInfo = BeginAsyncStateMachineTypeBuilder(WorkloadCoreMethodName, asyncMethodBuilderType, runnableBuilder);
            var (asyncStateMachineTypeBuilder, publicFields, (ilBuilder, endTryLabel, returnLabel, stateLocal, thisLocal, returnDefaultLocal)) = builderInfo;
            var (stateField, builderField, _) = publicFields;

            // Field declaration order matches Roslyn (and `<>u__N` numbering follows declaration order):
            // 1) <>u__1 — first awaiter type to appear in the IL (always ValueTaskAwaiter<bool> here, used
            //    by GetIsComplete and SetResultAndGetIsComplete);
            // 2) hoisted user locals in source declaration order (lastItem, startedClock);
            // 3) the synthetic enumerator local (named in the template so it's <enumerator>5__N, not
            //    <>7__wrap{N});
            // 4) <>u__N for any subsequent awaiter type that doesn't already match an existing field —
            //    Roslyn dedupes by type, so we reuse <>u__1 when MoveNextAsync also returns ValueTaskAwaiter<bool>.
            var workloadContinuerAwaiterField = asyncStateMachineTypeBuilder.DefineField("<>u__1", typeof(ValueTaskAwaiter<bool>), FieldAttributes.Private);
            var lastItemField = asyncStateMachineTypeBuilder.DefineField("<lastItem>5__2", itemType, FieldAttributes.Private);
            var startedClockField = asyncStateMachineTypeBuilder.DefineField("<startedClock>5__3", typeof(StartedClock), FieldAttributes.Private);
            var enumeratorField = asyncStateMachineTypeBuilder.DefineField("<enumerator>5__4", enumerableInfo.EnumeratorType, FieldAttributes.Private);
            int nextAwaiterOrdinal = 2;
            var moveNextAwaiterField = enumerableInfo.MoveNextAwaiterType == workloadContinuerAwaiterField.FieldType
                ? workloadContinuerAwaiterField
                : asyncStateMachineTypeBuilder.DefineField($"<>u__{nextAwaiterOrdinal++}", enumerableInfo.MoveNextAwaiterType, FieldAttributes.Private);
            var disposeAwaiterField = enumerableInfo.DisposeAsyncMethod is null
                ? null
                : enumerableInfo.DisposeAwaiterType == workloadContinuerAwaiterField.FieldType
                    ? workloadContinuerAwaiterField
                    : enumerableInfo.DisposeAwaiterType == moveNextAwaiterField.FieldType
                        ? moveNextAwaiterField
                        : asyncStateMachineTypeBuilder.DefineField($"<>u__{nextAwaiterOrdinal++}", enumerableInfo.DisposeAwaiterType!, FieldAttributes.Private);

            EmitMoveNextImpl();
            var asyncStateMachineType = CompleteAsyncStateMachineType(asyncMethodBuilderType, builderInfo);

            var workloadCoreMethod = EmitAsyncCallerStub(WorkloadCoreMethodName, asyncStateMachineType, publicFields);
            startWorkloadMethod = EmitAsyncSingleCall(StartWorkloadMethodName, typeof(AsyncVoidMethodBuilder), workloadCoreMethod, SetupCleanupKind.Other);

            void EmitMoveNextImpl()
            {
                // === States === (assigned in order each await first appears in code-flow, matching Roslyn)
                //   0  → resuming GetIsComplete
                //   1  → resuming MoveNextAsync (foreach loop)
                //   2  → resuming DisposeAsync (only when DisposeAsync exists; runs before SetResult)
                //   3  → resuming SetResultAndGetIsComplete
                // When DisposeAsync is absent, SetResult takes state 2 instead (no gap).
                const int StateGetIsComplete = 0;
                const int StateMoveNextAsync = 1;
                int StateDisposeAsync = 2;
                int StateSetResult = enumerableInfo.DisposeAsyncMethod is null ? 2 : 3;

                // Local declaration order matches Roslyn so the EmitsSameIL var-by-var diff lines up:
                // 1) awaiter then awaitable temps for the first await (Roslyn declares awaiter before
                //    awaitable for synthetic `await expr` with no named result);
                // 2) ClockSpan elapsed temp for the SetResult arg — Roslyn forces a local because the
                //    template puts a DCE.KeepAliveWithoutBoxing call between the elapsed read and the
                //    await suspension;
                // 3) the named source local `enumerable` (always — Roslyn declares one even for reference
                //    types because `unsafe { enumerable = ... }` is a separate assignment statement);
                // 4) optional default-value materializers for GetAsyncEnumerator's optional params
                //    (typically a CancellationToken local pre-declared here so EmitDefaultArgsForOptionalParameters
                //    can reuse it rather than declaring a new local mid-emit and shifting indexes);
                // 5) MoveNext awaiter/awaitable temps — REUSED from V_3/V_4 when types match (Roslyn dedupes
                //    by type), otherwise new locals;
                // 6) DisposeAsync awaiter/awaitable temps (if applicable);
                // 7) the loop-decrement long, then the catch-block Exception local.
                var isCompleteAwaiterLocal = ilBuilder.DeclareLocal(typeof(ValueTaskAwaiter<bool>));
                var isCompleteAwaitableLocal = ilBuilder.DeclareLocal(typeof(ValueTask<bool>));
                var elapsedLocal = ilBuilder.DeclareLocal(typeof(ClockSpan));
                var enumerableLocal = ilBuilder.DeclareLocal(enumerableInfo.WorkloadReturnType);
                var defaultArgLocals = enumerableInfo.GetAsyncEnumeratorMethod
                    .GetParameters()
                    .DistinctBy(p => p.ParameterType)
                    .ToDictionary(p => p.ParameterType, p => ilBuilder.DeclareLocal(p.ParameterType));
                var moveNextAwaiterLocal = moveNextAwaiterField.FieldType == isCompleteAwaiterLocal.LocalType
                    ? isCompleteAwaiterLocal
                    : ilBuilder.DeclareLocal(moveNextAwaiterField.FieldType);
                var moveNextAwaitableLocal = enumerableInfo.MoveNextAsyncMethod.ReturnType.IsValueType
                    ? (enumerableInfo.MoveNextAsyncMethod.ReturnType == isCompleteAwaitableLocal.LocalType
                        ? isCompleteAwaitableLocal
                        : ilBuilder.DeclareLocal(enumerableInfo.MoveNextAsyncMethod.ReturnType))
                    : null;
                LocalBuilder? disposeAwaiterLocal = null;
                LocalBuilder? disposeAwaitableLocal = null;
                if (enumerableInfo.DisposeAsyncMethod is not null)
                {
                    // Same awaiter-then-awaitable order as the GetIsComplete pattern (Roslyn always emits
                    // the awaiter local first for synthetic awaits).
                    disposeAwaiterLocal = ilBuilder.DeclareLocal(disposeAwaiterField!.FieldType);
                    disposeAwaitableLocal = enumerableInfo.DisposeAsyncMethod.ReturnType.IsValueType
                        ? ilBuilder.DeclareLocal(enumerableInfo.DisposeAsyncMethod.ReturnType)
                        : null;
                }
                var invokeCountLocal = ilBuilder.DeclareLocal(typeof(long));
                var exceptionLocal = ilBuilder.DeclareLocal(typeof(Exception));

                var getIsCompleteContinuationLabel = ilBuilder.DefineLabel();
                var getIsCompleteGetResultLabel = ilBuilder.DefineLabel();
                var startClockLabel = ilBuilder.DefineLabel();
                var callBenchmarkLabel = ilBuilder.DefineLabel();
                var callBenchmarkLoopLabel = ilBuilder.DefineLabel();
                var moveNextLoopLabel = ilBuilder.DefineLabel();
                var moveNextContinuationLabel = ilBuilder.DefineLabel();
                var moveNextGetResultLabel = ilBuilder.DefineLabel();
                var startDisposeLabel = ilBuilder.DefineLabel();
                var disposeContinuationLabel = ilBuilder.DefineLabel();
                var disposeGetResultLabel = ilBuilder.DefineLabel();
                var setResultContinuationLabel = ilBuilder.DefineLabel();
                var setResultGetResultLabel = ilBuilder.DefineLabel();

                // Roslyn preamble: ldloc state; ldc.i4.{maxState}; pop; pop; nop. The constant is the
                // largest dispatch state (3 when DisposeAsync exists, 2 otherwise) — Roslyn's switch
                // emit pre-pushes it as part of bounds-check elimination scaffolding.
                ilBuilder.EmitLdloc(stateLocal);
                ilBuilder.EmitLdc_I4(enumerableInfo.DisposeAsyncMethod is null ? 2 : 3);
                ilBuilder.Emit(OpCodes.Pop);
                ilBuilder.Emit(OpCodes.Pop);
                ilBuilder.Emit(OpCodes.Nop);

                ilBuilder.BeginExceptionBlock();
                {
                    // ===== State dispatch =====
                    // OpCodes.Switch matches what Roslyn emits for sequential resume states. The targets
                    // array is [state-0 → ..., state-1 → ..., state-2 → ..., state-3 → ...]; everything
                    // outside the range falls through to the first-time path.
                    ilBuilder.EmitLdloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Switch, enumerableInfo.DisposeAsyncMethod is null
                        ? [getIsCompleteContinuationLabel, moveNextContinuationLabel, setResultContinuationLabel]
                        : [getIsCompleteContinuationLabel, moveNextContinuationLabel, disposeContinuationLabel, setResultContinuationLabel]);

                    EmitGetIsCompleteAwait(StateGetIsComplete);

                    ilBuilder.MarkLabel(getIsCompleteContinuationLabel);
                    EmitResumeFromValueTaskBoolAwait(workloadContinuerAwaiterField, isCompleteAwaiterLocal);

                    ilBuilder.MarkLabel(getIsCompleteGetResultLabel);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetMethod(nameof(ValueTaskAwaiter<>.GetResult), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.Emit(OpCodes.Brfalse, startClockLabel);
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);
                    ilBuilder.Emit(OpCodes.Leave, endTryLabel);

                    // ===== while(true) { startClock → benchmark loop → DCE → SetResultAndGetIsComplete } =====
                    ilBuilder.MarkLabel(startClockLabel);
                    // lastItem = default; — reset accumulator per measurement
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(lastItemField);
                    // startedClock = ClockExtensions.Start(clock);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, clockField);
                    ilBuilder.Emit(OpCodes.Call, GetStartClockMethod());
                    ilBuilder.Emit(OpCodes.Stfld, startedClockField);
                    ilBuilder.Emit(OpCodes.Br, callBenchmarkLoopLabel);

                    // ===== Benchmark call: get enumerable, get enumerator, foreach loop, dispose =====
                    // Note: there is intentionally no nested CIL try/catch around the foreach. In CIL you can't
                    // branch into a protected region from outside, and the state-1 (MoveNextAsync) resume needs
                    // to land in the foreach loop body which would be inside such a region. If the foreach
                    // throws, the outer SetException catch surfaces it as a benchmark failure (DisposeAsync
                    // doesn't run in that case — acceptable trade-off given the benchmark process exits anyway).
                    ilBuilder.MarkLabel(callBenchmarkLabel);

                    // enumerable = workload(args); enumerator = enumerable.GetAsyncEnumerator(...);
                    // Roslyn defers loading `this` (for the stfld below) until AFTER the workload call so
                    // the call site doesn't carry an extra stack slot through it; mirroring that keeps the
                    // diff aligned.
                    if (!Descriptor.WorkloadMethod.IsStatic)
                        ilBuilder.EmitLdloc(thisLocal!);
                    EmitLoadArgFieldsForCall(ilBuilder, thisLocal);
                    ilBuilder.Emit(OpCodes.Call, Descriptor.WorkloadMethod);
                    ilBuilder.EmitStloc(enumerableLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    if (enumerableInfo.WorkloadReturnType.IsValueType)
                        ilBuilder.EmitLdloca(enumerableLocal);
                    else
                        ilBuilder.EmitLdloc(enumerableLocal);
                    EmitGetAsyncEnumeratorCall();
                    ilBuilder.Emit(OpCodes.Stfld, enumeratorField);

                    // "Goto check first" pattern Roslyn uses for `while (await MoveNextAsync()) { body }`:
                    // skip the body on first entry, run the MoveNextAsync check, and only enter the body
                    // when GetResult returns true. Conventional do-while-style structure (check at the
                    // bottom) would emit a different sequence of instructions and throw off the
                    // EmitsSameIL diff.
                    var loopBodyLabel = ilBuilder.DefineLabel();
                    ilBuilder.Emit(OpCodes.Br, moveNextLoopLabel);

                    // --- Loop body: lastItem = enumerator.Current; ---
                    ilBuilder.MarkLabel(loopBodyLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    EmitLoadEnumeratorForCall(enumeratorField);
                    EmitInvokeEnumeratorMethod(enumerableInfo.CurrentProperty.GetMethod!);
                    ilBuilder.Emit(OpCodes.Stfld, lastItemField);

                    ilBuilder.MarkLabel(moveNextLoopLabel);
                    // moveNextAwaitable = enumerator.MoveNextAsync();
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    EmitLoadEnumeratorForCall(enumeratorField);
                    EmitInvokeEnumeratorMethod(enumerableInfo.MoveNextAsyncMethod);
                    if (moveNextAwaitableLocal is null)
                    {
                        ilBuilder.Emit(OpCodes.Callvirt, enumerableInfo.MoveNextAwaitableType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                    }
                    else
                    {
                        ilBuilder.EmitStloc(moveNextAwaitableLocal);
                        ilBuilder.EmitLdloca(moveNextAwaitableLocal);
                        ilBuilder.Emit(OpCodes.Call, enumerableInfo.MoveNextAwaitableType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                    }
                    ilBuilder.EmitStloc(moveNextAwaiterLocal);
                    EmitLoadAwaiterAddressOrValue(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, moveNextAwaiterField.FieldType.GetProperty(nameof(TaskAwaiter.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!);
                    ilBuilder.Emit(OpCodes.Brtrue, moveNextGetResultLabel);
                    // state = 1; <>u__moveNext = awaiter; AwaitUnsafeOnCompleted(...); leave;
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdc_I4(StateMoveNextAsync);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Stfld, moveNextAwaiterField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, builderField);
                    ilBuilder.EmitLdloca(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, moveNextAwaiterField.FieldType, asyncStateMachineTypeBuilder));
                    ilBuilder.Emit(OpCodes.Leave, returnLabel);

                    // --- Resume from state 1 ---
                    ilBuilder.MarkLabel(moveNextContinuationLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, moveNextAwaiterField);
                    ilBuilder.EmitStloc(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(moveNextAwaiterField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_M1);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);

                    // --- GetResult --- if true, loop body again; else fall through to dispose.
                    ilBuilder.MarkLabel(moveNextGetResultLabel);
                    EmitLoadAwaiterAddressOrValue(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, moveNextAwaiterField.FieldType.GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)!);
                    ilBuilder.Emit(OpCodes.Brtrue, loopBodyLabel);

                    // ====== Inline finally: DisposeAsync (if applicable) ======
                    ilBuilder.MarkLabel(startDisposeLabel);
                    if (enumerableInfo.DisposeAsyncMethod is not null)
                    {
                        // disposeAwaitable = enumerator.DisposeAsync();
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        EmitLoadEnumeratorForCall(enumeratorField);
                        EmitInvokeEnumeratorMethod(enumerableInfo.DisposeAsyncMethod);
                        if (disposeAwaitableLocal is null)
                        {
                            ilBuilder.Emit(OpCodes.Callvirt, enumerableInfo.DisposeAwaitableType!.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                        }
                        else
                        {
                            ilBuilder.EmitStloc(disposeAwaitableLocal);
                            ilBuilder.EmitLdloca(disposeAwaitableLocal);
                            ilBuilder.Emit(OpCodes.Call, enumerableInfo.DisposeAwaitableType!.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                        }
                        ilBuilder.EmitStloc(disposeAwaiterLocal!);
                        EmitLoadAwaiterAddressOrValue(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Call, disposeAwaiterField!.FieldType.GetProperty(nameof(TaskAwaiter.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!);
                        ilBuilder.Emit(OpCodes.Brtrue, disposeGetResultLabel);
                        // state = 3; <>u__dispose = awaiter; AwaitUnsafeOnCompleted; leave
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.EmitLdc_I4(StateDisposeAsync);
                        ilBuilder.Emit(OpCodes.Dup);
                        ilBuilder.EmitStloc(stateLocal);
                        ilBuilder.Emit(OpCodes.Stfld, stateField);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.EmitLdloc(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Stfld, disposeAwaiterField);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldflda, builderField);
                        ilBuilder.EmitLdloca(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, disposeAwaiterField.FieldType, asyncStateMachineTypeBuilder));
                        ilBuilder.Emit(OpCodes.Leave, returnLabel);

                        // --- Resume from state 3 ---
                        ilBuilder.MarkLabel(disposeContinuationLabel);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldfld, disposeAwaiterField);
                        ilBuilder.EmitStloc(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.EmitSetFieldToDefault(disposeAwaiterField);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldc_I4_M1);
                        ilBuilder.Emit(OpCodes.Dup);
                        ilBuilder.EmitStloc(stateLocal);
                        ilBuilder.Emit(OpCodes.Stfld, stateField);

                        ilBuilder.MarkLabel(disposeGetResultLabel);
                        EmitLoadAwaiterAddressOrValue(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Call, disposeAwaiterField.FieldType.GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)!);
                    }

                    // Reset enumerator field so next iteration starts fresh
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(enumeratorField);

                    // --- Outer benchmark loop: if (--invokeCount >= 0) goto callBenchmarkLabel ---
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

                    // Match the template's source order: compute `elapsed` first, then DCE keep-alive on
                    // `lastItem`, then await SetResult(elapsed). Forcing `elapsed` through a local is what
                    // Roslyn does too — the DCE call between the elapsed read and the await prevents the
                    // C# compiler from keeping the ClockSpan on the stack.
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
                    ilBuilder.Emit(OpCodes.Call, typeof(StartedClock).GetMethod(nameof(StartedClock.GetElapsed), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(elapsedLocal);

                    // DCE keep-alive on the accumulated lastItem after the inner loop ends.
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, lastItemField);
                    var keepAliveMethod = typeof(DeadCodeEliminationHelper)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == nameof(DeadCodeEliminationHelper.KeepAliveWithoutBoxing)
                            && m.GetParameters().Length == 1
                            && !m.GetParameters()[0].ParameterType.IsByRef)
                        .MakeGenericMethod(itemType);
                    ilBuilder.Emit(OpCodes.Call, keepAliveMethod);

                    EmitSetResultAndGetIsCompleteAwait(StateSetResult);

                    ilBuilder.MarkLabel(setResultContinuationLabel);
                    EmitResumeFromValueTaskBoolAwait(workloadContinuerAwaiterField, isCompleteAwaiterLocal);

                    // Roslyn flow: brfalse continueLoop; (return-default + leave for true); continueLoop:
                    // clear startedClock; br loopStart. Same pattern as AsyncCoreEmitter — putting the
                    // startedClock clear in the continue-loop branch (rather than inline before the await
                    // suspend) keeps the IL aligned with what Roslyn emits for the template.
                    ilBuilder.MarkLabel(setResultGetResultLabel);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetMethod(nameof(ValueTaskAwaiter<>.GetResult), BindingFlags.Public | BindingFlags.Instance)!);
                    var continueLoopLabel = ilBuilder.DefineLabel();
                    ilBuilder.Emit(OpCodes.Brfalse, continueLoopLabel);
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);
                    ilBuilder.Emit(OpCodes.Leave, endTryLabel);
                    ilBuilder.MarkLabel(continueLoopLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(startedClockField);
                    ilBuilder.Emit(OpCodes.Br, startClockLabel);
                }
                ilBuilder.BeginCatchBlock(typeof(Exception));
                {
                    ilBuilder.EmitStloc(exceptionLocal);
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAndValueTaskSourceField);
                    ilBuilder.EmitLdloc(exceptionLocal);
                    ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.SetException), [typeof(Exception)])!);
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);

                    ilBuilder.EndExceptionBlock();
                }

                // -- Helper sub-emitters local to this function --

                void EmitGetIsCompleteAwait(int state)
                {
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAndValueTaskSourceField);
                    ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.GetIsComplete), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaitableLocal);
                    ilBuilder.EmitLdloca(isCompleteAwaitableLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTask<bool>).GetMethod(nameof(ValueTask<>.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaiterLocal);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetProperty(nameof(ValueTaskAwaiter<>.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!);
                    ilBuilder.Emit(OpCodes.Brtrue, getIsCompleteGetResultLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdc_I4(state);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Stfld, workloadContinuerAwaiterField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, builderField);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, typeof(ValueTaskAwaiter<bool>), asyncStateMachineTypeBuilder));
                    ilBuilder.Emit(OpCodes.Leave, returnLabel);
                }

                void EmitSetResultAndGetIsCompleteAwait(int state)
                {
                    // elapsed is now in the elapsed local (computed before the DCE call).
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadContinuerAndValueTaskSourceField);
                    ilBuilder.EmitLdloc(elapsedLocal);
                    ilBuilder.Emit(OpCodes.Callvirt, typeof(WorkloadValueTaskSource).GetMethod(nameof(WorkloadValueTaskSource.SetResultAndGetIsComplete), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaitableLocal);
                    ilBuilder.EmitLdloca(isCompleteAwaitableLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTask<bool>).GetMethod(nameof(ValueTask<>.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.EmitStloc(isCompleteAwaiterLocal);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetProperty(nameof(ValueTaskAwaiter<>.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!);
                    ilBuilder.Emit(OpCodes.Brtrue, setResultGetResultLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdc_I4(state);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Stfld, workloadContinuerAwaiterField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, builderField);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, typeof(ValueTaskAwaiter<bool>), asyncStateMachineTypeBuilder));
                    ilBuilder.Emit(OpCodes.Leave, returnLabel);
                }

                void EmitResumeFromValueTaskBoolAwait(FieldInfo awaiterField, LocalBuilder awaiterLocal)
                {
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, awaiterField);
                    ilBuilder.EmitStloc(awaiterLocal);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitSetFieldToDefault(awaiterField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldc_I4_M1);
                    ilBuilder.Emit(OpCodes.Dup);
                    ilBuilder.EmitStloc(stateLocal);
                    ilBuilder.Emit(OpCodes.Stfld, stateField);
                }

                void EmitGetAsyncEnumeratorCall()
                {
                    EmitDefaultArgsForOptionalParameters(enumerableInfo.GetAsyncEnumeratorMethod);
                    var opCode = enumerableInfo.IsInterfaceDispatch || !enumerableInfo.WorkloadReturnType.IsValueType
                        ? OpCodes.Callvirt
                        : OpCodes.Call;
                    ilBuilder.Emit(opCode, enumerableInfo.GetAsyncEnumeratorMethod);
                }

                void EmitInvokeEnumeratorMethod(MethodInfo method)
                {
                    EmitDefaultArgsForOptionalParameters(method);
                    ilBuilder.Emit(enumerableInfo.EnumeratorType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
                }

                void EmitLoadEnumeratorForCall(FieldInfo field)
                {
                    ilBuilder.Emit(enumerableInfo.EnumeratorType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, field);
                }

                void EmitDefaultArgsForOptionalParameters(MethodInfo method)
                {
                    foreach (var p in method.GetParameters())
                    {
                        if (p.ParameterType.IsValueType
                            && defaultArgLocals.TryGetValue(p.ParameterType, out var preDeclared))
                        {
                            // Reuse the local pre-declared at the top of the method so the variable order
                            // stays aligned with Roslyn's output (the alternative — DeclareLocal here —
                            // would push the index down to wherever this emit happens to run).
                            ilBuilder.EmitLdloca(preDeclared);
                            ilBuilder.Emit(OpCodes.Initobj, p.ParameterType);
                            ilBuilder.EmitLdloc(preDeclared);
                        }
                        else if (p.ParameterType.IsValueType)
                        {
                            var local = ilBuilder.DeclareLocal(p.ParameterType);
                            ilBuilder.EmitLdloca(local);
                            ilBuilder.Emit(OpCodes.Initobj, p.ParameterType);
                            ilBuilder.EmitLdloc(local);
                        }
                        else
                        {
                            ilBuilder.Emit(OpCodes.Ldnull);
                        }
                    }
                }

                void EmitLoadAwaiterAddressOrValue(LocalBuilder awaiterLocal)
                {
                    if (awaiterLocal.LocalType.IsValueType)
                        ilBuilder.EmitLdloca(awaiterLocal);
                    else
                        ilBuilder.EmitLdloc(awaiterLocal);
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        // Resolution: figure out which methods/types the await foreach pattern binds to for the
        // workload's return type. Mirrors the C# compiler's resolution rules — pattern first, then
        // interface fallback.
        // -----------------------------------------------------------------------------------------

        private sealed record EnumerableInfo(
            Type WorkloadReturnType,
            Type EnumeratorType,
            Type ItemType,
            MethodInfo GetAsyncEnumeratorMethod,
            MethodInfo MoveNextAsyncMethod,
            Type MoveNextAwaitableType,
            Type MoveNextAwaiterType,
            PropertyInfo CurrentProperty,
            MethodInfo? DisposeAsyncMethod,
            Type? DisposeAwaitableType,
            Type? DisposeAwaiterType,
            bool IsInterfaceDispatch);

        private static EnumerableInfo ResolveEnumerableInfo(Type workloadReturnType)
        {
            // Pattern first: a public instance GetAsyncEnumerator with all-optional parameters.
            var patternGetAsyncEnumerator = workloadReturnType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(IAsyncEnumerable<>.GetAsyncEnumerator)
                    && m.GetParameters().All(p => p.IsOptional));
            MethodInfo getAsyncEnumeratorMethod;
            bool isInterfaceDispatch;
            if (patternGetAsyncEnumerator != null)
            {
                getAsyncEnumeratorMethod = patternGetAsyncEnumerator;
                isInterfaceDispatch = false;
            }
            else
            {
                Type? iface = null;
                if (workloadReturnType.IsGenericType && workloadReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    iface = workloadReturnType;
                }
                else
                {
                    foreach (var i in workloadReturnType.GetInterfaces())
                    {
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                        {
                            iface = i;
                            break;
                        }
                    }
                }
                if (iface is null)
                {
                    throw new NotSupportedException($"Type {workloadReturnType.GetDisplayName()} is not an async enumerable.");
                }
                getAsyncEnumeratorMethod = iface.GetMethod(nameof(IAsyncEnumerable<>.GetAsyncEnumerator))!;
                isInterfaceDispatch = true;
            }

            var enumeratorType = getAsyncEnumeratorMethod.ReturnType;
            var moveNextAsyncMethod = enumeratorType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(IAsyncEnumerator<>.MoveNextAsync) && m.GetParameters().All(p => p.IsOptional))
                ?? throw new NotSupportedException($"Enumerator type {enumeratorType.GetDisplayName()} does not expose MoveNextAsync.");
            var moveNextAwaitableType = moveNextAsyncMethod.ReturnType;
            var moveNextAwaiterType = moveNextAwaitableType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!.ReturnType;

            var currentProperty = enumeratorType.GetProperty(nameof(IAsyncEnumerator<>.Current), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new NotSupportedException($"Enumerator type {enumeratorType.GetDisplayName()} does not expose Current.");
            var itemType = currentProperty.PropertyType;

            // DisposeAsync is optional for the await-foreach pattern. Roslyn matches a public instance
            // method named DisposeAsync whose parameters are all optional and whose awaiter's GetResult
            // returns void; otherwise it falls back to the IAsyncDisposable interface dispatch.
            MethodInfo? disposeAsyncMethod = enumeratorType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(IAsyncDisposable.DisposeAsync)
                    && m.GetParameters().All(p => p.IsOptional)
                    && m.ReturnType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)
                        ?.ReturnType
                        .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)
                        ?.ReturnType == typeof(void));
            if (disposeAsyncMethod is null && typeof(IAsyncDisposable).IsAssignableFrom(enumeratorType))
            {
                disposeAsyncMethod = typeof(IAsyncDisposable).GetMethod(nameof(IAsyncDisposable.DisposeAsync));
            }
            Type? disposeAwaitableType = disposeAsyncMethod?.ReturnType;
            Type? disposeAwaiterType = disposeAwaitableType?.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!.ReturnType;

            return new EnumerableInfo(
                workloadReturnType,
                enumeratorType,
                itemType,
                getAsyncEnumeratorMethod,
                moveNextAsyncMethod,
                moveNextAwaitableType,
                moveNextAwaiterType,
                currentProperty,
                disposeAsyncMethod,
                disposeAwaitableType,
                disposeAwaiterType,
                isInterfaceDispatch);
        }
    }
}
