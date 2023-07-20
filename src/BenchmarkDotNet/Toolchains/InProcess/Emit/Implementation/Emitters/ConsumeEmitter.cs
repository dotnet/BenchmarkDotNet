using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using JetBrains.Annotations;
using Perfolizer.Horology;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal abstract class ConsumeEmitter
    {
        public static ConsumeEmitter GetConsumeEmitter(ConsumableTypeInfo consumableTypeInfo)
        {
            if (consumableTypeInfo == null)
                throw new ArgumentNullException(nameof(consumableTypeInfo));

            if (consumableTypeInfo.IsAwaitable)
                return new TaskConsumeEmitter(consumableTypeInfo);
            if (consumableTypeInfo.IsVoid)
                return new VoidConsumeEmitter(consumableTypeInfo);
            if (consumableTypeInfo.IsByRef)
                return new ByRefConsumeEmitter(consumableTypeInfo);
            if (consumableTypeInfo.IsConsumable)
                return new ConsumableConsumeEmitter(consumableTypeInfo);
            return new NonConsumableConsumeEmitter(consumableTypeInfo);
        }

        protected ConsumeEmitter(ConsumableTypeInfo consumableTypeInfo)
        {
            if (consumableTypeInfo == null)
                throw new ArgumentNullException(nameof(consumableTypeInfo));

            ConsumableInfo = consumableTypeInfo;
        }

        protected ConsumableTypeInfo ConsumableInfo { get; }

        protected ILGenerator IlBuilder { get; private set; }
        protected MethodBuilder ActionMethodBuilder { get; private set; }
        protected MethodInfo ActionInvokeMethod { get; private set; }
        protected RunnableActionKind? ActionKind { get; private set; }

        [AssertionMethod]
        private void AssertNoBuilder()
        {
            if (IlBuilder != null)
                throw new InvalidOperationException("Bug: emit action logic is broken. Expects that IlBuilder != null");

            if (ActionMethodBuilder != null)
                throw new InvalidOperationException(
                    $"Bug: emit action logic is broken. {nameof(ActionMethodBuilder)} is not null.");

            if (ActionInvokeMethod != null)
                throw new InvalidOperationException(
                    $"Bug: emit action logic is broken. {nameof(ActionInvokeMethod)} is not null.");

            if (ActionKind != null)
                throw new InvalidOperationException(
                    $"Bug: emit action logic is broken. {nameof(ActionKind)} is not null.");
        }

        [AssertionMethod]
        private void AssertHasBuilder(ILGenerator ilBuilder)
        {
            if (IlBuilder != ilBuilder)
                throw new InvalidOperationException(
                    "Bug: emit action logic is broken. Expects that IlBuilder is same as passed one.");

            if (ActionMethodBuilder == null)
                throw new InvalidOperationException(
                    $"Bug: emit action logic is broken. {nameof(ActionMethodBuilder)} is null.");

            if (ActionInvokeMethod == null)
                throw new InvalidOperationException(
                    $"Bug: emit action logic is broken. {nameof(ActionInvokeMethod)} is null.");

            if (ActionKind != RunnableActionKind.Overhead && ActionKind != RunnableActionKind.Workload)
                throw new InvalidOperationException(
                    $"Bug: emit action logic is broken. Unknown {nameof(ActionKind)} value: {ActionKind}.");
        }

        public void OnDefineFields(TypeBuilder runnableBuilder)
        {
            AssertNoBuilder();

            OnDefineFieldsOverride(runnableBuilder);
        }

        protected virtual void OnDefineFieldsOverride(TypeBuilder runnableBuilder)
        {
        }

        public void OnEmitMembers(TypeBuilder runnableBuilder)
        {
            AssertNoBuilder();

            OnEmitMembersOverride(runnableBuilder);
        }

        protected virtual void OnEmitMembersOverride(TypeBuilder runnableBuilder)
        {
        }

        public void OnEmitCtorBody(ConstructorBuilder constructorBuilder, ILGenerator ilBuilder, RunnableEmitter runnableEmitter)
        {
            AssertNoBuilder();

            OnEmitCtorBodyOverride(constructorBuilder, ilBuilder, runnableEmitter);
        }

        protected virtual void OnEmitCtorBodyOverride(ConstructorBuilder constructorBuilder, ILGenerator ilBuilder, RunnableEmitter runnableEmitter)
        {
        }

        public void DeclareDisassemblyDiagnoserLocals(ILGenerator ilBuilder)
        {
            AssertNoBuilder();

            DeclareDisassemblyDiagnoserLocalsOverride(ilBuilder);
        }

        protected virtual void DeclareDisassemblyDiagnoserLocalsOverride(ILGenerator ilBuilder)
        {
        }

        public void EmitDisassemblyDiagnoserReturnDefault(ILGenerator ilBuilder)
        {
            AssertNoBuilder();

            EmitDisassemblyDiagnoserReturnDefaultOverride(ilBuilder);
        }

        protected virtual void EmitDisassemblyDiagnoserReturnDefaultOverride(ILGenerator ilBuilder)
        {
        }

        public void BeginEmitAction(
            MethodBuilder actionMethodBuilder,
            ILGenerator ilBuilder,
            MethodInfo actionInvokeMethod,
            RunnableActionKind actionKind)
        {
            if (actionMethodBuilder.IsStatic)
                throw new NotSupportedException($"The {actionMethodBuilder} method should be instance method.");

            AssertNoBuilder();

            IlBuilder = ilBuilder;
            ActionMethodBuilder = actionMethodBuilder;
            ActionInvokeMethod = actionInvokeMethod;
            ActionKind = actionKind;

            BeginEmitActionOverride(IlBuilder);
        }

        protected virtual void BeginEmitActionOverride(ILGenerator ilBuilder)
        {
        }

        public void CompleteEmitAction(ILGenerator ilBuilder)
        {
            AssertHasBuilder(ilBuilder);

            CompleteEmitActionOverride(ilBuilder);

            IlBuilder = null;
            ActionMethodBuilder = null;
            ActionInvokeMethod = null;
            ActionKind = null;
        }

        protected virtual void CompleteEmitActionOverride(ILGenerator ilBuilder)
        {
        }

        public void DeclareActionLocals(ILGenerator ilBuilder)
        {
            AssertHasBuilder(ilBuilder);

            DeclareActionLocalsOverride(ilBuilder);
        }

        protected virtual void DeclareActionLocalsOverride(ILGenerator ilBuilder)
        {
        }

        public void EmitActionBeforeLoop(ILGenerator ilBuilder)
        {
            AssertHasBuilder(ilBuilder);

            EmitActionBeforeLoopOverride(ilBuilder);
        }

        protected virtual void EmitActionBeforeLoopOverride(ILGenerator ilBuilder)
        {
        }

        public void EmitActionAfterLoop(ILGenerator ilBuilder)
        {
            AssertHasBuilder(ilBuilder);

            EmitActionAfterLoopOverride(ilBuilder);
        }

        protected virtual void EmitActionAfterLoopOverride(ILGenerator ilBuilder)
        {
        }

        public void EmitActionBeforeCall(ILGenerator ilBuilder)
        {
            AssertHasBuilder(ilBuilder);

            EmitActionBeforeCallOverride(ilBuilder);
        }

        protected virtual void EmitActionBeforeCallOverride(ILGenerator ilBuilder)
        {
        }

        public void EmitActionAfterCall(ILGenerator ilBuilder)
        {
            AssertHasBuilder(ilBuilder);

            EmitActionAfterCallOverride(ilBuilder);
        }

        protected virtual void EmitActionAfterCallOverride(ILGenerator ilBuilder)
        {
        }

        public virtual MethodBuilder EmitActionImpl(RunnableEmitter runnableEmitter, string methodName, RunnableActionKind actionKind, int unrollFactor)
        {
            FieldInfo actionDelegateField;
            MethodInfo actionInvokeMethod;
            switch (actionKind)
            {
                case RunnableActionKind.Overhead:
                    actionDelegateField = runnableEmitter.overheadDelegateField;
                    actionInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(runnableEmitter.overheadDelegateType);
                    break;
                case RunnableActionKind.Workload:
                    actionDelegateField = runnableEmitter.workloadDelegateField;
                    actionInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(runnableEmitter.workloadDelegateType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actionKind), actionKind, null);
            }

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

            // Emit impl
            var ilBuilder = actionMethodBuilder.GetILGenerator();
            BeginEmitAction(actionMethodBuilder, ilBuilder, actionInvokeMethod, actionKind);

            // init locals
            var argLocals = runnableEmitter.EmitDeclareArgLocals(ilBuilder);
            DeclareActionLocals(ilBuilder);
            var startedClockLocal = ilBuilder.DeclareLocal(typeof(StartedClock));
            var indexLocal = ilBuilder.DeclareLocal(typeof(long));

            // load fields
            runnableEmitter.EmitLoadArgFieldsToLocals(ilBuilder, argLocals);
            EmitActionBeforeLoop(ilBuilder);

            // start clock
            /*
                // var startedClock = Perfolizer.Horology.ClockExtensions.Start(clock);
                IL_0000: ldarg.2
                IL_0001: call valuetype Perfolizer.Horology.StartedClock Perfolizer.Horology.ClockExtensions::Start(class Perfolizer.Horology.IClock)
                IL_0006: stloc.0
             */
            ilBuilder.EmitLdarg(clockArg);
            ilBuilder.Emit(OpCodes.Call, runnableEmitter.startClockMethod);
            ilBuilder.EmitStloc(startedClockLocal);

            // loop
            var loopStartLabel = ilBuilder.DefineLabel();
            var loopHeadLabel = ilBuilder.DefineLabel();
            ilBuilder.EmitLoopBeginFromLocToArg(loopStartLabel, loopHeadLabel, indexLocal, toArg);
            {
                /*
                    // overheadDelegate();
                    IL_0005: ldarg.0
                    IL_0006: ldfld class BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate BenchmarkDotNet.Autogenerated.Runnable_0::overheadDelegate
                    IL_000b: callvirt instance void BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate::Invoke()
                    // -or-
                    // consumer.Consume(overheadDelegate(_argField));
                    IL_000c: ldarg.0
                    IL_000d: ldfld class [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer BenchmarkDotNet.Autogenerated.Runnable_0::consumer
                    IL_0012: ldarg.0
                    IL_0013: ldfld class BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate BenchmarkDotNet.Autogenerated.Runnable_0::overheadDelegate
                    IL_0018: ldloc.0
                    IL_0019: callvirt instance int32 BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate::Invoke(int64)
                    IL_001e: callvirt instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer::Consume(int32)
                 */
                for (int u = 0; u < unrollFactor; u++)
                {
                    EmitActionBeforeCall(ilBuilder);

                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, actionDelegateField);
                    ilBuilder.EmitInstanceCallThisValueOnStack(null, actionInvokeMethod, argLocals);

                    EmitActionAfterCall(ilBuilder);
                }
            }
            ilBuilder.EmitLoopEndFromLocToArg(loopStartLabel, loopHeadLabel, indexLocal, toArg);

            EmitActionAfterLoop(ilBuilder);
            CompleteEmitAction(ilBuilder);

            /*
                // return new System.Threading.Tasks.ValueTask<Perfolizer.Horology.ClockSpan>(startedClock.GetElapsed());
                IL_0021: ldloca.s 0
                IL_0023: call instance valuetype Perfolizer.Horology.ClockSpan Perfolizer.Horology.StartedClock::GetElapsed()
                IL_0028: newobj instance void valuetype [System.Private.CoreLib]System.Threading.Tasks.ValueTask`1<valuetype Perfolizer.Horology.ClockSpan>::.ctor(!0)
                IL_002d: ret
             */
            ilBuilder.EmitLdloca(startedClockLocal);
            ilBuilder.Emit(OpCodes.Call, runnableEmitter.getElapsedMethod);
            var ctor = typeof(ValueTask<ClockSpan>).GetConstructor(new[] { typeof(ClockSpan) });
            ilBuilder.Emit(OpCodes.Newobj, ctor);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }
    }
}