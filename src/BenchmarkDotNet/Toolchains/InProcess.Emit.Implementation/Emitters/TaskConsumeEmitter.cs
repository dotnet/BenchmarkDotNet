﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using Perfolizer.Horology;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal class TaskConsumeEmitter : ConsumeEmitter
    {
        private MethodInfo overheadKeepAliveWithoutBoxingMethod;
        private MethodInfo getResultMethod;

        private LocalBuilder disassemblyDiagnoserLocal;
        /*
            private readonly BenchmarkDotNet.Helpers.ManualResetValueTaskSource<Perfolizer.Horology.ClockSpan> valueTaskSource = new BenchmarkDotNet.Helpers.ManualResetValueTaskSource<Perfolizer.Horology.ClockSpan>();
            private System.Int64 repeatsRemaining;
            private readonly System.Action continuation;
            private Perfolizer.Horology.StartedClock startedClock;
            private $AwaiterTypeName$ currentAwaiter;
        */
        private FieldBuilder valueTaskSourceField;
        private FieldBuilder repeatsRemainingField;
        private FieldBuilder continuationField;
        private FieldBuilder startedClockField;
        private FieldBuilder currentAwaiterField;

        private MethodBuilder overheadActionImplMethod;
        private MethodBuilder workloadActionImplMethod;
        private MethodBuilder runTaskMethod;
        private MethodBuilder continuationMethod;
        private MethodBuilder setExceptionMethod;

        public TaskConsumeEmitter(ConsumableTypeInfo consumableTypeInfo) : base(consumableTypeInfo)
        {
        }

        protected override void OnDefineFieldsOverride(TypeBuilder runnableBuilder)
        {
            overheadKeepAliveWithoutBoxingMethod = typeof(DeadCodeEliminationHelper).GetMethods()
                .First(m => m.Name == nameof(DeadCodeEliminationHelper.KeepAliveWithoutBoxing)
                    && !m.GetParameterTypes().First().IsByRef)
                .MakeGenericMethod(ConsumableInfo.OverheadMethodReturnType);

            valueTaskSourceField = runnableBuilder.DefineField(ValueTaskSourceFieldName, typeof(Helpers.ManualResetValueTaskSource<ClockSpan>), FieldAttributes.Private | FieldAttributes.InitOnly);
            repeatsRemainingField = runnableBuilder.DefineField(RepeatsRemainingFieldName, typeof(long), FieldAttributes.Private);
            continuationField = runnableBuilder.DefineField(ContinuationFieldName, typeof(Action), FieldAttributes.Private | FieldAttributes.InitOnly);
            startedClockField = runnableBuilder.DefineField(StartedClockFieldName, typeof(StartedClock), FieldAttributes.Private);
            // (Value)TaskAwaiter(<T>)
            currentAwaiterField = runnableBuilder.DefineField(CurrentAwaiterFieldName,
                ConsumableInfo.WorkloadMethodReturnType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance).ReturnType,
                FieldAttributes.Private);
            getResultMethod = currentAwaiterField.FieldType.GetMethod(nameof(TaskAwaiter.GetResult), BindingFlagsAllInstance);
        }

        protected override void DeclareDisassemblyDiagnoserLocalsOverride(ILGenerator ilBuilder)
        {
            // optional local if default(T) uses .initobj
            disassemblyDiagnoserLocal = ilBuilder.DeclareOptionalLocalForReturnDefault(ConsumableInfo.WorkloadMethodReturnType);
        }

        protected override void EmitDisassemblyDiagnoserReturnDefaultOverride(ILGenerator ilBuilder)
        {
            ilBuilder.EmitReturnDefault(ConsumableInfo.WorkloadMethodReturnType, disassemblyDiagnoserLocal);
        }

        protected override void OnEmitCtorBodyOverride(ConstructorBuilder constructorBuilder, ILGenerator ilBuilder)
        {
            var ctor = typeof(Helpers.ManualResetValueTaskSource<ClockSpan>).GetConstructor(Array.Empty<Type>());
            if (ctor == null)
                throw new InvalidOperationException($"Cannot get default .ctor for {typeof(Helpers.ManualResetValueTaskSource<ClockSpan>)}");

            /*
                // valueTaskSourceField = new BenchmarkDotNet.Helpers.ManualResetValueTaskSource<Perfolizer.Horology.ClockSpan>();
                IL_0000: ldarg.0
                IL_0001: newobj instance void class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan>::.ctor()
                IL_0006: stfld class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::valueTaskSource
             */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Newobj, ctor);
            ilBuilder.Emit(OpCodes.Stfld, valueTaskSourceField);
            // continuation = __Continuation;
            ilBuilder.EmitSetDelegateToThisField(continuationField, continuationMethod);
        }

        public override MethodBuilder EmitActionImpl(RunnableEmitter runnableEmitter, string methodName, RunnableActionKind actionKind, int unrollFactor)
        {
            MethodBuilder actionImpl = actionKind switch
            {
                RunnableActionKind.Overhead => EmitOverheadActionImpl(runnableEmitter),
                RunnableActionKind.Workload => EmitWorkloadActionImpl(runnableEmitter),
                _ => throw new ArgumentOutOfRangeException(nameof(actionKind), actionKind, null),
            };

            /*
                .method private hidebysig
                    instance valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan> WorkloadActionNoUnroll (
                        int64 invokeCount,
                        class Perfolizer.Horology.IClock clock
                    ) cil managed
            */
            var toArg = new EmitParameterInfo(0, InvokeCountParamName, typeof(long));
            var clockArg = new EmitParameterInfo(1, ClockParamName, typeof(IClock));
            var actionMethodBuilder = runnableEmitter.runnableBuilder.DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                toArg, clockArg);
            toArg.SetMember(actionMethodBuilder);
            clockArg.SetMember(actionMethodBuilder);

            var ilBuilder = actionMethodBuilder.GetILGenerator();

            /*
                // return WorkloadActionImpl(invokeCount, clock);
                IL_0000: ldarg.0
                IL_0001: ldarg.1
                IL_0002: ldarg.2
                IL_0003: call instance valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::WorkloadActionImpl(int64, class Perfolizer.Horology.IClock)
                IL_0008: ret
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.EmitLdarg(toArg);
            ilBuilder.EmitLdarg(clockArg);
            ilBuilder.Emit(OpCodes.Call, actionImpl);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }

        private MethodBuilder EmitOverheadActionImpl(RunnableEmitter runnableEmitter)
        {
            if (overheadActionImplMethod != null)
            {
                return overheadActionImplMethod;
            }

            FieldInfo actionDelegateField = runnableEmitter.overheadDelegateField;
            MethodInfo actionInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(runnableEmitter.overheadDelegateType);

            /*
                .method private hidebysig
                    instance valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan> OverheadActionImpl (
                        int64 invokeCount,
                        class Perfolizer.Horology.IClock clock
                    ) cil managed
            */
            var toArg = new EmitParameterInfo(0, InvokeCountParamName, typeof(long));
            var clockArg = new EmitParameterInfo(1, ClockParamName, typeof(IClock));
            var actionMethodBuilder = runnableEmitter.runnableBuilder.DefineNonVirtualInstanceMethod(
                OverheadActionImplMethodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                toArg, clockArg);
            toArg.SetMember(actionMethodBuilder);
            clockArg.SetMember(actionMethodBuilder);

            var ilBuilder = actionMethodBuilder.GetILGenerator();

            // init locals
            var valueLocal = ilBuilder.DeclareLocal(ConsumableInfo.OverheadMethodReturnType);
            var argLocals = runnableEmitter.EmitDeclareArgLocals(ilBuilder);
            var indexLocal = ilBuilder.DeclareLocal(typeof(long));

            /*
                // repeatsRemaining = invokeCount;
                IL_0000: ldarg.0
                IL_0001: ldarg.1
                IL_0002: stfld int64 BenchmarkRunner_0::repeatsRemaining
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.EmitLdarg(toArg);
            ilBuilder.Emit(OpCodes.Stfld, repeatsRemainingField);
            /*
                // Task<int> value = default;
                IL_0007: ldnull
                IL_0008: stloc.0
            */
            ilBuilder.EmitSetLocalToDefault(valueLocal);
            /*
                // startedClock = Perfolizer.Horology.ClockExtensions.Start(clock);
                IL_0009: ldarg.0
                IL_000a: ldarg.2
                IL_000b: call valuetype Perfolizer.Horology.StartedClock Perfolizer.Horology.ClockExtensions::Start(class Perfolizer.Horology.IClock)
                IL_0010: stfld valuetype Perfolizer.Horology.StartedClock BenchmarkRunner_0::startedClock
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.EmitLdarg(clockArg);
            ilBuilder.Emit(OpCodes.Call, runnableEmitter.startClockMethod);
            ilBuilder.Emit(OpCodes.Stfld, startedClockField);

            // try { ... }
            ilBuilder.BeginExceptionBlock();
            {
                // load fields
                runnableEmitter.EmitLoadArgFieldsToLocals(ilBuilder, argLocals);

                // while (--repeatsRemaining >= 0) { ... }
                var loopStartLabel = ilBuilder.DefineLabel();
                var loopHeadLabel = ilBuilder.DefineLabel();
                ilBuilder.EmitLoopBeginFromFldTo0(loopStartLabel, loopHeadLabel);
                {
                    /*
                        // value = overheadDelegate();
                        IL_0017: ldarg.0
                        IL_0018: ldfld class BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate BenchmarkDotNet.Autogenerated.Runnable_0::overheadDelegate
                        IL_001d: callvirt instance void BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate::Invoke()
                        IL_0022: stloc.0
                    */
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, actionDelegateField);
                    ilBuilder.EmitInstanceCallThisValueOnStack(null, actionInvokeMethod, argLocals);
                    ilBuilder.EmitStloc(valueLocal);
                }
                ilBuilder.EmitLoopEndFromFldTo0(loopStartLabel, loopHeadLabel, repeatsRemainingField, indexLocal);
            }
            // catch (System.Exception) { ... }
            ilBuilder.BeginCatchBlock(typeof(Exception));
            {
                // IL_003b: pop
                ilBuilder.Emit(OpCodes.Pop);
                /*
                    // BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                    IL_003c: ldloc.0
                    IL_003d: call void BenchmarkDotNet.Engines.DeadCodeEliminationHelper::KeepAliveWithoutBoxing<class [System.Private.CoreLib]System.Threading.Tasks.Task`1<int32>>(!!0)
                */
                ilBuilder.EmitStaticCall(overheadKeepAliveWithoutBoxingMethod, valueLocal);
                // IL_0042: rethrow
                ilBuilder.Emit(OpCodes.Rethrow);
            }
            ilBuilder.EndExceptionBlock();

            /*
                // return new System.Threading.Tasks.ValueTask<Perfolizer.Horology.ClockSpan>(startedClock.GetElapsed());
                IL_0044: ldarg.0
                IL_0045: ldflda valuetype Perfolizer.Horology.StartedClock BenchmarkRunner_0::startedClock
                IL_004a: call instance valuetype Perfolizer.Horology.ClockSpan Perfolizer.Horology.StartedClock::GetElapsed()
                IL_004f: newobj instance void valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan>::.ctor(!0)
                IL_0054: ret
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
            ilBuilder.Emit(OpCodes.Call, runnableEmitter.getElapsedMethod);
            var ctor = typeof(ValueTask<ClockSpan>).GetConstructor(new[] { typeof(ClockSpan) });
            ilBuilder.Emit(OpCodes.Newobj, ctor);
            ilBuilder.Emit(OpCodes.Ret);

            return overheadActionImplMethod = actionMethodBuilder;
        }

        private MethodBuilder EmitWorkloadActionImpl(RunnableEmitter runnableEmitter)
        {
            if (workloadActionImplMethod != null)
            {
                return workloadActionImplMethod;
            }

            setExceptionMethod = EmitSetExceptionImpl(runnableEmitter);
            runTaskMethod = EmitRunTaskImpl(runnableEmitter);
            continuationMethod = EmitContinuationImpl(runnableEmitter);

            /*
                .method private hidebysig
                    instance valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan> WorkloadActionImpl (
                        int64 invokeCount,
                        class Perfolizer.Horology.IClock clock
                    ) cil managed
            */
            var toArg = new EmitParameterInfo(0, InvokeCountParamName, typeof(long));
            var clockArg = new EmitParameterInfo(1, ClockParamName, typeof(IClock));
            var actionMethodBuilder = runnableEmitter.runnableBuilder.DefineNonVirtualInstanceMethod(
                WorkloadActionImplMethodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnParameter(typeof(ValueTask<ClockSpan>)),
                toArg, clockArg);
            toArg.SetMember(actionMethodBuilder);
            clockArg.SetMember(actionMethodBuilder);

            var ilBuilder = actionMethodBuilder.GetILGenerator();

            /*
                // repeatsRemaining = invokeCount;
                IL_0000: ldarg.0
                IL_0001: ldarg.1
                IL_0002: stfld int64 BenchmarkRunner_0::repeatsRemaining
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.EmitLdarg(toArg);
            ilBuilder.Emit(OpCodes.Stfld, repeatsRemainingField);
            /*
                // valueTaskSource.Reset();
                IL_0007: ldarg.0
                IL_0008: ldfld class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::valueTaskSource
                IL_000d: callvirt instance void class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan>::Reset()
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, valueTaskSourceField);
            var resetMethod = valueTaskSourceField.FieldType.GetMethod(nameof(Helpers.ManualResetValueTaskSource<ClockSpan>.Reset), BindingFlagsPublicInstance);
            ilBuilder.Emit(OpCodes.Callvirt, resetMethod);
            /*
                // startedClock = Perfolizer.Horology.ClockExtensions.Start(clock);
                IL_0012: ldarg.0
                IL_0013: ldarg.2
                IL_0014: call valuetype Perfolizer.Horology.StartedClock Perfolizer.Horology.ClockExtensions::Start(class Perfolizer.Horology.IClock)
                IL_0019: stfld valuetype Perfolizer.Horology.StartedClock BenchmarkRunner_0::startedClock
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.EmitLdarg(clockArg);
            ilBuilder.Emit(OpCodes.Call, runnableEmitter.startClockMethod);
            ilBuilder.Emit(OpCodes.Stfld, startedClockField);
            /*
                // __RunTask();
                IL_001e: ldarg.0
                IL_001f: call instance void BenchmarkRunner_0::__RunTask()
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Call, runTaskMethod);
            /*
                // return new System.Threading.Tasks.ValueTask<Perfolizer.Horology.ClockSpan>(valueTaskSource, valueTaskSource.Version);
                IL_0024: ldarg.0
                IL_0025: ldfld class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::valueTaskSource
                IL_002a: ldarg.0
                IL_002b: ldfld class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::valueTaskSource
                IL_0030: callvirt instance int16 class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan>::get_Version()
                IL_0035: newobj instance void valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan>::.ctor(class [System.Private.CoreLib]System.Threading.Tasks.Sources.IValueTaskSource`1<!0>, int16)
                IL_003a: ret
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, valueTaskSourceField);
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, valueTaskSourceField);
            var getVersionMethod = valueTaskSourceField.FieldType.GetProperty(nameof(Helpers.ManualResetValueTaskSource<ClockSpan>.Version), BindingFlagsPublicInstance).GetGetMethod(true);
            ilBuilder.Emit(OpCodes.Callvirt, getVersionMethod);
            var ctor = actionMethodBuilder.ReturnType.GetConstructor(new[] { valueTaskSourceField.FieldType, getVersionMethod.ReturnType });
            ilBuilder.Emit(OpCodes.Newobj, ctor);
            ilBuilder.Emit(OpCodes.Ret);

            return workloadActionImplMethod = actionMethodBuilder;
        }

        private MethodBuilder EmitRunTaskImpl(RunnableEmitter runnableEmitter)
        {
            /*
                .method private hidebysig
                    instance void __RunTask () cil managed
            */
            var actionMethodBuilder = runnableEmitter.runnableBuilder.DefineNonVirtualInstanceMethod(
                RunTaskMethodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnVoidParameter());

            var ilBuilder = actionMethodBuilder.GetILGenerator();

            FieldInfo actionDelegateField = runnableEmitter.workloadDelegateField;
            MethodInfo actionInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(runnableEmitter.workloadDelegateType);

            // init locals
            //.locals init (
            //    [0] valuetype Perfolizer.Horology.ClockSpan clockspan,
            //    // [1] valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<int32>, // If ValueTask
            //    [1] int64,
            //    [2] class [System.Private.CoreLib]System.Exception e
            //)
            var clockspanLocal = ilBuilder.DeclareLocal(typeof(ClockSpan));
            var argLocals = runnableEmitter.EmitDeclareArgLocals(ilBuilder);
            LocalBuilder maybeValueTaskLocal = actionInvokeMethod.ReturnType.IsValueType
                ? ilBuilder.DeclareLocal(actionInvokeMethod.ReturnType)
                : null;
            var indexLocal = ilBuilder.DeclareLocal(typeof(long));
            var exceptionLocal = ilBuilder.DeclareLocal(typeof(Exception));

            var returnLabel = ilBuilder.DefineLabel();

            // try { ... }
            ilBuilder.BeginExceptionBlock();
            {
                // load fields
                runnableEmitter.EmitLoadArgFieldsToLocals(ilBuilder, argLocals);

                // while (--repeatsRemaining >= 0) { ... }
                var loopStartLabel = ilBuilder.DefineLabel();
                var loopHeadLabel = ilBuilder.DefineLabel();
                ilBuilder.EmitLoopBeginFromFldTo0(loopStartLabel, loopHeadLabel);
                {
                    /*
                        // currentAwaiter = workloadDelegate().GetAwaiter();
                        IL_0002: ldarg.0
                        IL_0003: ldarg.0
                        IL_0004: ldfld class [System.Private.CoreLib]System.Func`1<valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<int32>> BenchmarkRunner_0::workloadDelegate
                        IL_0009: callvirt instance !0 class [System.Private.CoreLib]System.Func`1<valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<int32>>::Invoke()
                        IL_000e: stloc.1
                        IL_000f: ldloca.s 1
                        IL_0011: call instance valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<!0> valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<int32>::GetAwaiter()
                        IL_0016: stfld valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                    */
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, actionDelegateField);
                    ilBuilder.EmitInstanceCallThisValueOnStack(null, actionInvokeMethod, argLocals);
                    ilBuilder.EmitInstanceCallThisValueOnStack(maybeValueTaskLocal, ConsumableInfo.WorkloadMethodReturnType.GetMethod(nameof(Task.GetAwaiter), BindingFlagsAllInstance));
                    ilBuilder.Emit(OpCodes.Stfld, currentAwaiterField);
                    /*
                        // if (!currentAwaiter.IsCompleted)
                        IL_001b: ldarg.0
                        IL_001c: ldflda valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                        IL_0021: call instance bool valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32>::get_IsCompleted()
                        IL_0026: brtrue.s IL_003b
                    */
                    var isCompletedLabel = ilBuilder.DefineLabel();

                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, currentAwaiterField);
                    ilBuilder.Emit(OpCodes.Call, currentAwaiterField.FieldType.GetProperty(nameof(TaskAwaiter.IsCompleted), BindingFlagsAllInstance).GetGetMethod(true));
                    ilBuilder.Emit(OpCodes.Brtrue, isCompletedLabel);
                    {
                        /*
                            // currentAwaiter.UnsafeOnCompleted(continuation);
                            IL_0028: ldarg.0
                            IL_0029: ldflda valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                            IL_002e: ldarg.0
                            IL_002f: ldfld class [System.Private.CoreLib]System.Action BenchmarkRunner_0::continuation
                            IL_0034: call instance void valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32>::UnsafeOnCompleted(class [System.Private.CoreLib]System.Action)
                        */
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldflda, currentAwaiterField);
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                        ilBuilder.Emit(OpCodes.Ldfld, continuationField);
                        ilBuilder.Emit(OpCodes.Call, currentAwaiterField.FieldType.GetMethod(nameof(TaskAwaiter.UnsafeOnCompleted), BindingFlagsAllInstance));
                        // return;
                        ilBuilder.Emit(OpCodes.Leave, returnLabel);
                    }
                    ilBuilder.MarkLabel(isCompletedLabel);
                    /*
                        // currentAwaiter.GetResult();
                        IL_003b: ldarg.0
                        IL_003c: ldflda valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                        IL_0041: call instance !0 valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32>::GetResult()
                        IL_0046: pop
                    */
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldflda, currentAwaiterField);
                    ilBuilder.Emit(OpCodes.Call, getResultMethod);
                    if (getResultMethod.ReturnType != typeof(void))
                    {
                        ilBuilder.Emit(OpCodes.Pop);
                    }
                }
                ilBuilder.EmitLoopEndFromFldTo0(loopStartLabel, loopHeadLabel, repeatsRemainingField, indexLocal);
            }
            // catch (System.Exception) { ... }
            ilBuilder.BeginCatchBlock(typeof(Exception));
            {
                /*
                    // __SetException(e);
                    IL_005f: stloc.3
                    IL_0060: ldarg.0
                    IL_0061: ldloc.3
                    IL_0062: call instance void BenchmarkRunner_0::__SetException(class [System.Private.CoreLib]System.Exception)
                */
                ilBuilder.EmitStloc(exceptionLocal);
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.EmitLdloc(exceptionLocal);
                ilBuilder.Emit(OpCodes.Call, setExceptionMethod);
                // return;
                ilBuilder.Emit(OpCodes.Leave, returnLabel);
            }
            ilBuilder.EndExceptionBlock();

            /*
                // var clockspan = startedClock.GetElapsed();
                IL_0069: ldarg.0
                IL_006a: ldflda valuetype Perfolizer.Horology.StartedClock BenchmarkRunner_0::startedClock
                IL_006f: call instance valuetype Perfolizer.Horology.ClockSpan Perfolizer.Horology.StartedClock::GetElapsed()
                IL_0074: stloc.0
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
            ilBuilder.Emit(OpCodes.Call, runnableEmitter.getElapsedMethod);
            ilBuilder.EmitStloc(clockspanLocal);
            /*
                // currentAwaiter = default;
                IL_0075: ldarg.0
                IL_0076: ldflda valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                IL_007b: initobj valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32>
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, currentAwaiterField);
            ilBuilder.Emit(OpCodes.Initobj, currentAwaiterField.FieldType);
            /*
                // startedClock = default(Perfolizer.Horology.StartedClock);
                IL_0081: ldarg.0
                IL_0082: ldflda valuetype Perfolizer.Horology.StartedClock BenchmarkRunner_0::startedClock
                IL_0087: initobj Perfolizer.Horology.StartedClock
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
            ilBuilder.Emit(OpCodes.Initobj, startedClockField.FieldType);
            /*
                // valueTaskSource.SetResult(clockspan);
                IL_008d: ldarg.0
                IL_008e: ldfld class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::valueTaskSource
                IL_0093: ldloc.0
                IL_0094: callvirt instance void class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan>::SetResult(!0)
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, valueTaskSourceField);
            ilBuilder.EmitLdloc(clockspanLocal);
            ilBuilder.Emit(OpCodes.Callvirt, valueTaskSourceField.FieldType.GetMethod(nameof(Helpers.ManualResetValueTaskSource<ClockSpan>.SetResult), BindingFlagsPublicInstance));

            ilBuilder.MarkLabel(returnLabel);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }

        private MethodBuilder EmitContinuationImpl(RunnableEmitter runnableEmitter)
        {
            /*
                .method private hidebysig
                    instance void __Continuation () cil managed
            */
            var actionMethodBuilder = runnableEmitter.runnableBuilder.DefineNonVirtualInstanceMethod(
                ContinuationMethodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnVoidParameter());

            var ilBuilder = actionMethodBuilder.GetILGenerator();

            // init locals
            var exceptionLocal = ilBuilder.DeclareLocal(typeof(Exception));

            var returnLabel = ilBuilder.DefineLabel();

            // try { ... }
            ilBuilder.BeginExceptionBlock();
            {
                /*
                    // currentAwaiter.GetResult();
                    IL_0000: ldarg.0
                    IL_0001: ldflda valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                    IL_0006: call instance !0 valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32>::GetResult()
                    IL_000b: pop
                */
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.Emit(OpCodes.Ldflda, currentAwaiterField);
                ilBuilder.Emit(OpCodes.Call, getResultMethod);
                if (getResultMethod.ReturnType != typeof(void))
                {
                    ilBuilder.Emit(OpCodes.Pop);
                }
            }
            // catch (System.Exception e) { ... }
            ilBuilder.BeginCatchBlock(typeof(Exception));
            {
                // IL_000e: stloc.0
                ilBuilder.EmitStloc(exceptionLocal);
                /*
                    // __SetException(e);
                    IL_000f: ldarg.0
                    IL_0010: ldloc.0
                    IL_0011: call instance void BenchmarkRunner_0::__SetException(class [System.Private.CoreLib]System.Exception)
                */
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.EmitLdloc(exceptionLocal);
                ilBuilder.Emit(OpCodes.Call, setExceptionMethod);
                // return;
                ilBuilder.Emit(OpCodes.Leave, returnLabel);
            }
            ilBuilder.EndExceptionBlock();

            /*
                // __RunTask();
                IL_0018: ldarg.0
                IL_0019: call instance void BenchmarkRunner_0::__RunTask()
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Call, runTaskMethod);

            // return;
            ilBuilder.MarkLabel(returnLabel);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }

        private MethodBuilder EmitSetExceptionImpl(RunnableEmitter runnableEmitter)
        {
            /*
                .method private hidebysig
                    instance void __SetException (
                        class [System.Private.CoreLib]System.Exception e
                    ) cil managed
            */
            var exceptionArg = new EmitParameterInfo(0, "e", typeof(Exception));
            var actionMethodBuilder = runnableEmitter.runnableBuilder.DefineNonVirtualInstanceMethod(
                SetExceptionMethodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnVoidParameter(),
                exceptionArg);
            exceptionArg.SetMember(actionMethodBuilder);

            var ilBuilder = actionMethodBuilder.GetILGenerator();
            /*
                // currentAwaiter = default;
                IL_0000: ldarg.0
                IL_0001: ldflda valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32> BenchmarkRunner_0::currentAwaiter
                IL_0006: initobj valuetype [System.Private.CoreLib]System.Runtime.CompilerServices.ValueTaskAwaiter`1<int32>
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, currentAwaiterField);
            ilBuilder.Emit(OpCodes.Initobj, currentAwaiterField.FieldType);
            /*
                // startedClock = default(Perfolizer.Horology.StartedClock);
                IL_000c: ldarg.0
                IL_000d: ldflda valuetype Perfolizer.Horology.StartedClock BenchmarkRunner_0::startedClock
                IL_0012: initobj Perfolizer.Horology.StartedClock
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldflda, startedClockField);
            ilBuilder.Emit(OpCodes.Initobj, startedClockField.FieldType);
            /*
                // valueTaskSource.SetException(e);
                IL_0018: ldarg.0
                IL_0019: ldfld class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan> BenchmarkRunner_0::valueTaskSource
                IL_001e: ldarg.1
                IL_001f: callvirt instance void class BenchmarkDotNet.Helpers.ManualResetValueTaskSource`1<valuetype Perfolizer.Horology.ClockSpan>::SetException(class [System.Private.CoreLib]System.Exception)
                IL_0024: ret
            */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, valueTaskSourceField);
            ilBuilder.EmitLdarg(exceptionArg);
            var setExceptionMethod = valueTaskSourceField.FieldType.GetMethod(nameof(Helpers.ManualResetValueTaskSource<ClockSpan>.SetException), BindingFlagsPublicInstance);
            ilBuilder.Emit(OpCodes.Callvirt, setExceptionMethod);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }
    }
}