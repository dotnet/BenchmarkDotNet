using System;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal abstract class ConsumeEmitter
    {
        public static ConsumeEmitter GetConsumeEmitter(ConsumableTypeInfo consumableTypeInfo)
        {
            if (consumableTypeInfo == null)
                throw new ArgumentNullException(nameof(consumableTypeInfo));

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

        protected ILGenerator? IlBuilder { get; private set; }
        protected MethodBuilder? ActionMethodBuilder { get; private set; }
        protected MethodInfo? ActionInvokeMethod { get; private set; }
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

        public void OnEmitCtorBody(ConstructorBuilder constructorBuilder, ILGenerator ilBuilder)
        {
            AssertNoBuilder();

            OnEmitCtorBodyOverride(constructorBuilder, ilBuilder);
        }

        protected virtual void OnEmitCtorBodyOverride(ConstructorBuilder constructorBuilder, ILGenerator ilBuilder)
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
    }
}