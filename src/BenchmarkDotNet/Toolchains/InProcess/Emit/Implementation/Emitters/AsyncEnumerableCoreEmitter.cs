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
        private readonly AsyncEnumerableInfo enumerableInfo;
        private readonly MethodInfo? disposeAsyncMethod;
        private readonly AwaitableInfo? disposeAwaitableInfo;

        public AsyncEnumerableCoreEmitter(BuildPartition buildPartition, ModuleBuilder moduleBuilder, BenchmarkBuildInfo benchmark, AsyncEnumerableInfo enumerableInfo)
            : base(buildPartition, moduleBuilder, benchmark)
        {
            this.enumerableInfo = enumerableInfo;

            // DisposeAsync is optional for the await-foreach pattern. Roslyn matches a public instance
            // method named DisposeAsync whose parameters are all optional and whose return type satisfies
            // the awaitable pattern with a void GetResult; otherwise it falls back to the IAsyncDisposable
            // interface dispatch.
            foreach (var candidate in enumerableInfo.EnumeratorType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (candidate.Name == nameof(IAsyncDisposable.DisposeAsync)
                    && candidate.GetParameters().All(p => p.IsOptional)
                    && candidate.ReturnType.IsAwaitable(out var awaitable)
                    && awaitable.ResultType == typeof(void))
                {
                    disposeAsyncMethod = candidate;
                    disposeAwaitableInfo = awaitable;
                    break;
                }
            }
            if (disposeAsyncMethod is null && typeof(IAsyncDisposable).IsAssignableFrom(enumerableInfo.EnumeratorType))
            {
                disposeAsyncMethod = typeof(IAsyncDisposable).GetMethod(nameof(IAsyncDisposable.DisposeAsync))!;
                disposeAsyncMethod.ReturnType.IsAwaitable(out disposeAwaitableInfo);
            }
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
            // 2) hoisted user locals in source declaration order (startedClock);
            // 3) <>7__wrap2 — synthesized hoisted enumerator (Roslyn names it `<>7__wrap2` because the
            //    `await foreach` lowering captures the GetAsyncEnumerator result as an unnamed wrap);
            // 4) when DisposeAsync exists, <>7__wrap3 (captured catch object) and <>7__wrap4 (unused state
            //    discriminator that Roslyn emits as part of its try/finally lowering);
            // 5) <>u__N for any subsequent awaiter type that doesn't already match an existing field —
            //    Roslyn dedupes by type, so we reuse <>u__1 when MoveNextAsync also returns ValueTaskAwaiter<bool>.
            var workloadContinuerAwaiterField = asyncStateMachineTypeBuilder.DefineField("<>u__1", typeof(ValueTaskAwaiter<bool>), FieldAttributes.Private);
            var startedClockField = asyncStateMachineTypeBuilder.DefineField("<startedClock>5__2", typeof(StartedClock), FieldAttributes.Private);
            var enumeratorField = asyncStateMachineTypeBuilder.DefineField("<>7__wrap2", enumerableInfo.EnumeratorType, FieldAttributes.Private);
            var capturedExceptionField = disposeAsyncMethod is null
                ? null
                : asyncStateMachineTypeBuilder.DefineField("<>7__wrap3", typeof(object), FieldAttributes.Private);
            var disposeDiscriminatorField = disposeAsyncMethod is null
                ? null
                : asyncStateMachineTypeBuilder.DefineField("<>7__wrap4", typeof(int), FieldAttributes.Private);
            int nextAwaiterOrdinal = 2;
            var moveNextAwaiterField = enumerableInfo.MoveNextAwaitable.AwaiterType == workloadContinuerAwaiterField.FieldType
                ? workloadContinuerAwaiterField
                : asyncStateMachineTypeBuilder.DefineField($"<>u__{nextAwaiterOrdinal++}", enumerableInfo.MoveNextAwaitable.AwaiterType, FieldAttributes.Private);
            var disposeAwaiterField = disposeAsyncMethod is null
                ? null
                : disposeAwaitableInfo!.AwaiterType == workloadContinuerAwaiterField.FieldType
                    ? workloadContinuerAwaiterField
                    : disposeAwaitableInfo!.AwaiterType == moveNextAwaiterField.FieldType
                        ? moveNextAwaiterField
                        : asyncStateMachineTypeBuilder.DefineField($"<>u__{nextAwaiterOrdinal++}", disposeAwaitableInfo!.AwaiterType!, FieldAttributes.Private);

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
                int StateSetResult = disposeAsyncMethod is null ? 2 : 3;

                // Local declaration order matches Roslyn so the EmitsSameIL var-by-var diff lines up:
                // 1) awaiter then awaitable temps for the first await (Roslyn declares awaiter before
                //    awaitable for synthetic `await expr` with no named result);
                // 2) the named source local `enumerable` (always — Roslyn declares one even for reference
                //    types because `unsafe { enumerable = ... }` is a separate assignment statement);
                // 3) optional default-value materializers for GetAsyncEnumerator's optional params
                //    (typically a CancellationToken local pre-declared here so EmitDefaultArgsForOptionalParameters
                //    can reuse it rather than declaring a new local mid-emit and shifting indexes);
                // 4) the named source local `item` for `var item = enumerator.Current;` — Roslyn declares
                //    it after `enumerable` (and after defaultArg materializers) but BEFORE the MoveNext
                //    awaiter/awaitable temps;
                // 5) caught-exception `object` local — only when DisposeAsync exists (the inner catch(object)
                //    handler that captures any exception thrown by the iteration uses this slot, and it is
                //    reused after the dispose await for the rethrow logic that reads <>7__wrap3);
                // 6) MoveNext awaiter/awaitable temps — REUSED from V_3/V_4 when types match (Roslyn dedupes
                //    by type), otherwise new locals;
                // 7) DisposeAsync awaiter/awaitable temps (if applicable);
                // 8) the loop-decrement long, then the catch-block Exception local.
                // The template inlines `startedClock.GetElapsed()` directly into the SetResult call site,
                // so no ClockSpan local is declared.
                var isCompleteAwaiterLocal = ilBuilder.DeclareLocal(typeof(ValueTaskAwaiter<bool>));
                var isCompleteAwaitableLocal = ilBuilder.DeclareLocal(typeof(ValueTask<bool>));
                // Roslyn only spills the workload result to a local when its return type is a value type
                // (needed to take an address for the instance GetAsyncEnumerator call). Reference-type
                // results stay on the stack and pass directly to GetAsyncEnumerator.
                var enumerableLocal = Descriptor.WorkloadMethod.ReturnType.IsValueType
                    ? ilBuilder.DeclareLocal(Descriptor.WorkloadMethod.ReturnType)
                    : null;
                var defaultArgLocals = enumerableInfo.GetAsyncEnumeratorMethod
                    .GetParameters()
                    .DistinctBy(p => p.ParameterType)
                    .ToDictionary(p => p.ParameterType, p => ilBuilder.DeclareLocal(p.ParameterType));
                var itemLocal = ilBuilder.DeclareLocal(enumerableInfo.ItemType);
                var moveNextAwaiterLocal = moveNextAwaiterField.FieldType == isCompleteAwaiterLocal.LocalType
                    ? isCompleteAwaiterLocal
                    : ilBuilder.DeclareLocal(moveNextAwaiterField.FieldType);
                var moveNextAwaitableLocal = enumerableInfo.MoveNextAsyncMethod.ReturnType.IsValueType
                    ? (enumerableInfo.MoveNextAsyncMethod.ReturnType == isCompleteAwaitableLocal.LocalType
                        ? isCompleteAwaitableLocal
                        : ilBuilder.DeclareLocal(enumerableInfo.MoveNextAsyncMethod.ReturnType))
                    : null;
                var caughtObjectLocal = disposeAsyncMethod is null
                    ? null
                    : ilBuilder.DeclareLocal(typeof(object));
                LocalBuilder? disposeAwaiterLocal = null;
                LocalBuilder? disposeAwaitableLocal = null;
                if (disposeAsyncMethod is not null)
                {
                    // Same awaiter-then-awaitable order as the GetIsComplete pattern (Roslyn always emits
                    // the awaiter local first for synthetic awaits).
                    disposeAwaiterLocal = ilBuilder.DeclareLocal(disposeAwaiterField!.FieldType);
                    disposeAwaitableLocal = disposeAsyncMethod.ReturnType.IsValueType
                        ? ilBuilder.DeclareLocal(disposeAsyncMethod.ReturnType)
                        : null;
                }
                var invokeCountLocal = ilBuilder.DeclareLocal(typeof(long));
                var exceptionLocal = ilBuilder.DeclareLocal(typeof(Exception));

                var getIsCompleteContinuationLabel = ilBuilder.DefineLabel();
                var getIsCompleteGetResultLabel = ilBuilder.DefineLabel();
                var startClockLabel = ilBuilder.DefineLabel();
                var callBenchmarkLabel = ilBuilder.DefineLabel();
                var callBenchmarkLoopLabel = ilBuilder.DefineLabel();
                var loopBodyLabel = ilBuilder.DefineLabel();
                var moveNextLoopLabel = ilBuilder.DefineLabel();
                var moveNextContinuationLabel = ilBuilder.DefineLabel();
                var moveNextGetResultLabel = ilBuilder.DefineLabel();
                // The following labels only matter when DisposeAsync exists — they're the await-foreach
                // try/finally lowering's skeleton (state-1 lands at `state1TargetLabel` which is the nop
                // right before the inner try opens; after the inner try-catch, control reaches
                // `afterInnerTryLabel` and runs the dispose-then-rethrow sequence).
                var state1TargetLabel = ilBuilder.DefineLabel();
                var afterInnerTryLabel = ilBuilder.DefineLabel();
                var skipRethrowLabel = ilBuilder.DefineLabel();
                var rethrowDispatchLabel = ilBuilder.DefineLabel();
                var startDisposeLabel = ilBuilder.DefineLabel();
                var disposeContinuationLabel = ilBuilder.DefineLabel();
                var disposeGetResultLabel = ilBuilder.DefineLabel();
                var setResultContinuationLabel = ilBuilder.DefineLabel();
                var setResultGetResultLabel = ilBuilder.DefineLabel();

                // Roslyn preamble: ldloc state; ldc.i4.{maxState}; pop; pop; nop. The constant is the
                // largest dispatch state (3 when DisposeAsync exists, 2 otherwise) — Roslyn's switch
                // emit pre-pushes it as part of bounds-check elimination scaffolding.
                ilBuilder.EmitLdloc(stateLocal);
                ilBuilder.EmitLdc_I4(disposeAsyncMethod is null ? 2 : 3);
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
                    // When DisposeAsync exists Roslyn wraps the iteration in a nested try whose body
                    // contains the MoveNext resume point — but CIL forbids branching into a protected
                    // region from outside it, so state-1 targets a `nop` just *before* the inner try and
                    // falls through into it; a redispatch inside the try then jumps to the actual
                    // moveNextContinuation. Without DisposeAsync there is no nested try and state-1
                    // targets the moveNext continuation directly.
                    ilBuilder.Emit(OpCodes.Switch, disposeAsyncMethod is null
                        ? [getIsCompleteContinuationLabel, moveNextContinuationLabel, setResultContinuationLabel]
                        : [getIsCompleteContinuationLabel, state1TargetLabel, disposeContinuationLabel, setResultContinuationLabel]);

                    EmitGetIsCompleteAwait(StateGetIsComplete);

                    ilBuilder.MarkLabel(getIsCompleteContinuationLabel);
                    EmitResumeFromValueTaskBoolAwait(workloadContinuerAwaiterField, isCompleteAwaiterLocal);

                    ilBuilder.MarkLabel(getIsCompleteGetResultLabel);
                    ilBuilder.EmitLdloca(isCompleteAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, typeof(ValueTaskAwaiter<bool>).GetMethod(nameof(ValueTaskAwaiter<>.GetResult), BindingFlags.Public | BindingFlags.Instance)!);
                    ilBuilder.Emit(OpCodes.Brfalse, startClockLabel);
                    ilBuilder.MaybeEmitSetLocalToDefault(returnDefaultLocal);
                    ilBuilder.Emit(OpCodes.Leave, endTryLabel);

                    // ===== while(true) { startClock → benchmark loop → SetResultAndGetIsComplete } =====
                    ilBuilder.MarkLabel(startClockLabel);
                    // startedClock = ClockExtensions.Start(clock);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, clockField);
                    ilBuilder.Emit(OpCodes.Call, GetStartClockMethod());
                    ilBuilder.Emit(OpCodes.Stfld, startedClockField);
                    ilBuilder.Emit(OpCodes.Br, callBenchmarkLoopLabel);

                    // ===== Benchmark call: get enumerable, get enumerator, await foreach, dispose =====
                    // When DisposeAsync exists Roslyn lowers the `await foreach` into a nested try/catch
                    // (the catch captures into <>7__wrap3) followed by a dispose-then-rethrow sequence;
                    // we mirror that exactly so the state-machine layout matches the template's IL.
                    // Without DisposeAsync there is no nested protected region and the iteration lives
                    // directly in the outer try.
                    ilBuilder.MarkLabel(callBenchmarkLabel);

                    // this.<>7__wrap2 = workload(args).GetAsyncEnumerator(...);
                    // Roslyn loads `this` (for the stfld) BEFORE evaluating the right-hand side, matching
                    // C# evaluation order for `this.field = expr`.
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    if (!Descriptor.WorkloadMethod.IsStatic)
                        ilBuilder.EmitLdloc(thisLocal!);
                    EmitLoadArgFieldsForCall(ilBuilder, thisLocal);
                    ilBuilder.Emit(OpCodes.Call, Descriptor.WorkloadMethod);
                    if (enumerableLocal is not null)
                    {
                        // Value-type result: spill so we can take its address for the instance call.
                        ilBuilder.EmitStloc(enumerableLocal);
                        ilBuilder.EmitLdloca(enumerableLocal);
                    }
                    EmitGetAsyncEnumeratorCall();
                    ilBuilder.Emit(OpCodes.Stfld, enumeratorField);

                    if (disposeAsyncMethod is not null)
                    {
                        // <>7__wrap3 = null; <>7__wrap4 = 0; — Roslyn initializes both before opening the
                        // inner try. <>7__wrap3 holds any exception caught by the try; <>7__wrap4 is the
                        // try/finally state discriminator that Roslyn emits but never actually reads back
                        // for this lowering shape (kept for IL-equivalence).
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldnull);
                        ilBuilder.Emit(OpCodes.Stfld, capturedExceptionField!);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldc_I4_0);
                        ilBuilder.Emit(OpCodes.Stfld, disposeDiscriminatorField!);

                        // state-1 jumps to this label (just before the inner try opens), then control
                        // falls through into the try at its first instruction.
                        ilBuilder.MarkLabel(state1TargetLabel);
                        ilBuilder.Emit(OpCodes.Nop);

                        ilBuilder.BeginExceptionBlock();
                        // Re-dispatch state-1 inside the inner protected region — CIL forbids branching
                        // into the try from outside, so the outer switch lands at the nop above and we
                        // jump to moveNextContinuationLabel here once execution is safely inside the try.
                        ilBuilder.EmitLdloc(stateLocal);
                        ilBuilder.EmitLdc_I4(StateMoveNextAsync);
                        ilBuilder.Emit(OpCodes.Beq_S, moveNextContinuationLabel);
                        ilBuilder.Emit(OpCodes.Br_S, moveNextLoopLabel);
                    }
                    else
                    {
                        // "Goto check first" pattern Roslyn uses for `while (await MoveNextAsync()) { body }`:
                        // skip the body on first entry, run the MoveNextAsync check, and only enter the
                        // body when GetResult returns true.
                        ilBuilder.Emit(OpCodes.Br, moveNextLoopLabel);
                    }

                    // --- Loop body: var item = enumerator.Current; DeadCodeEliminationHelper.KeepAliveWithoutBoxing(in item); ---
                    ilBuilder.MarkLabel(loopBodyLabel);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    EmitLoadEnumeratorForCall(enumeratorField);
                    EmitInvokeEnumeratorMethod(enumerableInfo.CurrentProperty.GetMethod!);
                    ilBuilder.EmitStloc(itemLocal);
                    ilBuilder.EmitLdloca(itemLocal);
                    var keepAliveInMethod = typeof(DeadCodeEliminationHelper)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == nameof(DeadCodeEliminationHelper.KeepAliveWithoutBoxing)
                            && m.GetParameters().Length == 1
                            && m.GetParameters()[0].ParameterType.IsByRef)
                        .MakeGenericMethod(enumerableInfo.ItemType);
                    ilBuilder.Emit(OpCodes.Call, keepAliveInMethod);

                    ilBuilder.MarkLabel(moveNextLoopLabel);
                    // moveNextAwaitable = enumerator.MoveNextAsync();
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    EmitLoadEnumeratorForCall(enumeratorField);
                    EmitInvokeEnumeratorMethod(enumerableInfo.MoveNextAsyncMethod);
                    if (moveNextAwaitableLocal is null)
                    {
                        ilBuilder.Emit(OpCodes.Callvirt, enumerableInfo.MoveNextAwaitable.GetAwaiterMethod);
                    }
                    else
                    {
                        ilBuilder.EmitStloc(moveNextAwaitableLocal);
                        ilBuilder.EmitLdloca(moveNextAwaitableLocal);
                        ilBuilder.Emit(OpCodes.Call, enumerableInfo.MoveNextAwaitable.GetAwaiterMethod);
                    }
                    ilBuilder.EmitStloc(moveNextAwaiterLocal);
                    EmitLoadAwaiterAddressOrValue(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, enumerableInfo.MoveNextAwaitable.IsCompletedProperty.GetMethod!);
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

                    // --- GetResult --- if true, loop body again; else fall through.
                    ilBuilder.MarkLabel(moveNextGetResultLabel);
                    EmitLoadAwaiterAddressOrValue(moveNextAwaiterLocal);
                    ilBuilder.Emit(OpCodes.Call, enumerableInfo.MoveNextAwaitable.GetResultMethod);
                    ilBuilder.Emit(OpCodes.Brtrue, loopBodyLabel);

                    if (disposeAsyncMethod is not null)
                    {
                        // catch (object) { <>7__wrap3 = caught; } — Roslyn's await-foreach lowering uses
                        // `catch (object)` (not `catch (Exception)`) so non-Exception throwables are also
                        // captured and rethrown after dispose. ILGenerator synthesizes `leave` instructions
                        // at the end of the try body (when transitioning to BeginCatchBlock) and at the end
                        // of the catch body (when EndExceptionBlock runs); both target the end-of-block
                        // marker which we re-mark as `afterInnerTryLabel` below.
                        ilBuilder.BeginCatchBlock(typeof(object));
                        ilBuilder.EmitStloc(caughtObjectLocal!);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.EmitLdloc(caughtObjectLocal!);
                        ilBuilder.Emit(OpCodes.Stfld, capturedExceptionField!);
                        ilBuilder.EndExceptionBlock();

                        // ====== After-foreach: optional await DisposeAsync, then rethrow if captured ======
                        ilBuilder.MarkLabel(afterInnerTryLabel);

                        // For REFERENCE-type enumerators (typically IAsyncEnumerator<T>): if the enumerator
                        // field is null we skip the dispose await entirely. Roslyn emits this `ldfld;
                        // brfalse` guard only for reference enumerators — a value-type Enumerator (struct)
                        // can never be null, and `ldfld; brfalse` on a multi-word struct is meaningless,
                        // so the value-type path goes straight to DisposeAsync.
                        ilBuilder.MarkLabel(startDisposeLabel);
                        if (!enumerableInfo.EnumeratorType.IsValueType)
                        {
                            ilBuilder.Emit(OpCodes.Ldarg_0);
                            ilBuilder.Emit(OpCodes.Ldfld, enumeratorField);
                            ilBuilder.Emit(OpCodes.Brfalse_S, skipRethrowLabel);
                        }

                        // disposeAwaitable = enumerator.DisposeAsync();
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        EmitLoadEnumeratorForCall(enumeratorField);
                        EmitInvokeEnumeratorMethod(disposeAsyncMethod);
                        if (disposeAwaitableLocal is null)
                        {
                            ilBuilder.Emit(OpCodes.Callvirt, disposeAwaitableInfo!.GetAwaiterMethod);
                        }
                        else
                        {
                            ilBuilder.EmitStloc(disposeAwaitableLocal);
                            ilBuilder.EmitLdloca(disposeAwaitableLocal);
                            ilBuilder.Emit(OpCodes.Call, disposeAwaitableInfo!.GetAwaiterMethod);
                        }
                        ilBuilder.EmitStloc(disposeAwaiterLocal!);
                        EmitLoadAwaiterAddressOrValue(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Call, disposeAwaitableInfo!.IsCompletedProperty.GetMethod!);
                        ilBuilder.Emit(OpCodes.Brtrue, disposeGetResultLabel);
                        // state = 2; <>u__dispose = awaiter; AwaitUnsafeOnCompleted; leave
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.EmitLdc_I4(StateDisposeAsync);
                        ilBuilder.Emit(OpCodes.Dup);
                        ilBuilder.EmitStloc(stateLocal);
                        ilBuilder.Emit(OpCodes.Stfld, stateField);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.EmitLdloc(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Stfld, disposeAwaiterField!);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldflda, builderField);
                        ilBuilder.EmitLdloca(disposeAwaiterLocal!);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Call, GetAwaitOnCompletedMethod(asyncMethodBuilderType, disposeAwaiterField!.FieldType, asyncStateMachineTypeBuilder));
                        ilBuilder.Emit(OpCodes.Leave, returnLabel);

                        // --- Resume from state 2 ---
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
                        ilBuilder.Emit(OpCodes.Call, disposeAwaitableInfo!.GetResultMethod);

                        // Rethrow path: if <>7__wrap3 holds a captured exception, rethrow it (via
                        // ExceptionDispatchInfo when it's a real Exception, raw `throw` otherwise so
                        // non-Exception payloads keep their original semantics).
                        ilBuilder.MarkLabel(skipRethrowLabel);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldfld, capturedExceptionField!);
                        ilBuilder.EmitStloc(caughtObjectLocal!);
                        ilBuilder.EmitLdloc(caughtObjectLocal!);
                        var afterRethrowLabel = ilBuilder.DefineLabel();
                        ilBuilder.Emit(OpCodes.Brfalse_S, afterRethrowLabel);
                        ilBuilder.EmitLdloc(caughtObjectLocal!);
                        ilBuilder.Emit(OpCodes.Isinst, typeof(Exception));
                        ilBuilder.Emit(OpCodes.Dup);
                        ilBuilder.Emit(OpCodes.Brtrue_S, rethrowDispatchLabel);
                        ilBuilder.EmitLdloc(caughtObjectLocal!);
                        ilBuilder.Emit(OpCodes.Throw);
                        ilBuilder.MarkLabel(rethrowDispatchLabel);
                        ilBuilder.Emit(OpCodes.Call, typeof(System.Runtime.ExceptionServices.ExceptionDispatchInfo).GetMethod(nameof(System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture))!);
                        ilBuilder.Emit(OpCodes.Callvirt, typeof(System.Runtime.ExceptionServices.ExceptionDispatchInfo).GetMethod(nameof(System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw), Type.EmptyTypes)!);
                        ilBuilder.MarkLabel(afterRethrowLabel);

                        // <>7__wrap3 = null; — clear the captured-exception field for the next iteration.
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldnull);
                        ilBuilder.Emit(OpCodes.Stfld, capturedExceptionField!);
                    }

                    // Reset enumerator field so next iteration starts fresh.
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

                    // The template inlines `startedClock.GetElapsed()` directly into SetResult — no
                    // intermediate ClockSpan local. EmitSetResultAndGetIsCompleteAwait emits the GetElapsed
                    // call as part of the SetResult argument sequence.
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
                    ilBuilder.Emit(OpCodes.Ldfld, workloadValueTaskSourceField);
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
                    ilBuilder.Emit(OpCodes.Ldfld, workloadValueTaskSourceField);
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
                    // Inline `startedClock.GetElapsed()` as the SetResult argument — the template no longer
                    // declares a ClockSpan elapsed local, so neither do we.
                    ilBuilder.EmitLdloc(thisLocal!);
                    ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);
                    ilBuilder.Emit(OpCodes.Ldfld, workloadValueTaskSourceField);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
                    ilBuilder.Emit(OpCodes.Call, typeof(StartedClock).GetMethod(nameof(StartedClock.GetElapsed), BindingFlags.Public | BindingFlags.Instance)!);
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
                    var opCode = enumerableInfo.IsInterfaceDispatch || !Descriptor.WorkloadMethod.ReturnType.IsValueType
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
    }
}
