using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal class NonConsumableConsumeEmitter : ConsumeEmitter
    {
        private MethodInfo overheadKeepAliveWithoutBoxingMethod;
        private MethodInfo nonGenericKeepAliveWithoutBoxingMethod;
        private LocalBuilder resultLocal;
        private LocalBuilder disassemblyDiagnoserLocal;

        public NonConsumableConsumeEmitter(ConsumableTypeInfo consumableTypeInfo) : base(consumableTypeInfo)
        {
        }

        protected override void OnEmitMembersOverride(TypeBuilder runnableBuilder)
        {
            overheadKeepAliveWithoutBoxingMethod = typeof(DeadCodeEliminationHelper).GetMethods()
                .First(m => m.Name == nameof(DeadCodeEliminationHelper.KeepAliveWithoutBoxing)
                    && !m.GetParameterTypes().First().IsByRef)
                .MakeGenericMethod(ConsumableInfo.OverheadMethodReturnType);

            // we must not simply use DeadCodeEliminationHelper.KeepAliveWithoutBoxing<T> because it's generic method
            // and stack-only types like Span<T> can not be generic type arguments http://adamsitnik.com/Span/#span-must-not-be-a-generic-type-argument
            nonGenericKeepAliveWithoutBoxingMethod = EmitNonGenericKeepAliveWithoutBoxing(
                NonGenericKeepAliveWithoutBoxingMethodName,
                runnableBuilder);
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

        private MethodBuilder EmitNonGenericKeepAliveWithoutBoxing(string methodName, TypeBuilder runnableBuilder)
        {
            /*
                method private hidebysig
                instance void NonGenericKeepAliveWithoutBoxing(
                    valuetype BenchmarkDotNet.Samples.CustomStructNonConsumable _
                ) cil managed noinlining
             */
            var valueArg = new EmitParameterInfo(
                0,
                DummyParamName,
                ConsumableInfo.WorkloadMethodReturnType);
            var methodBuilder = runnableBuilder.DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnVoidParameter(),
                valueArg)
                .SetNoInliningImplementationFlag();
            valueArg.SetMember(methodBuilder);

            var ilBuilder = methodBuilder.GetILGenerator();

            /*
                IL_0001: ret
             */
            ilBuilder.EmitVoidReturn(methodBuilder);

            return methodBuilder;
        }


        protected override void DeclareActionLocalsOverride(ILGenerator ilBuilder)
        {
            /*
                .locals init (
                    [2] int32
                )
                -or-
                .locals init (
                    [2] valuetype BenchmarkDotNet.Samples.CustomStructNonConsumable,
                )
             */
            if (ActionKind == RunnableActionKind.Overhead)
                resultLocal = ilBuilder.DeclareLocal(ConsumableInfo.OverheadMethodReturnType);
            else
                resultLocal = ilBuilder.DeclareLocal(ConsumableInfo.WorkloadMethodReturnType);
        }

        /// <summary>Emits the action before loop override.</summary>
        /// <param name="ilBuilder">The il builder.</param>
        /// <exception cref="ArgumentOutOfRangeException">EmitActionKind - null</exception>
        protected override void EmitActionBeforeLoopOverride(ILGenerator ilBuilder)
        {
            /*
                // int value = 0;
                IL_000e: ldc.i4.0
                IL_000f: stloc.2
                -or-
                // CustomStructNonConsumable _ = default(CustomStructNonConsumable);
                IL_000e: ldloca.s 2
                IL_0010: initobj BenchmarkDotNet.Samples.CustomStructNonConsumable
             */
            ilBuilder.EmitSetLocalToDefault(resultLocal);
        }

        protected override void EmitActionAfterCallOverride(ILGenerator ilBuilder)
        {
            // IL_0022: stloc.2
            ilBuilder.EmitStloc(resultLocal);
        }

        protected override void EmitActionAfterLoopOverride(ILGenerator ilBuilder)
        {
            /*
                // DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                IL_002c: ldloc.2
                IL_002d: call void [BenchmarkDotNet]BenchmarkDotNet.Engines.DeadCodeEliminationHelper::KeepAliveWithoutBoxing<int32>(!!0)
                -or-
                // NonGenericKeepAliveWithoutBoxing(_);
                IL_0032: ldarg.0
                IL_0033: ldloc.2
                IL_0034: call instance void BenchmarkDotNet.Autogenerated.Runnable_0::NonGenericKeepAliveWithoutBoxing(valuetype BenchmarkDotNet.Samples.CustomStructNonConsumable)
             */
            if (ActionKind == RunnableActionKind.Overhead)
            {
                ilBuilder.EmitStaticCall(overheadKeepAliveWithoutBoxingMethod, resultLocal);
            }
            else
            {
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.EmitInstanceCallThisValueOnStack(
                    null,
                    nonGenericKeepAliveWithoutBoxingMethod,
                    new[] { resultLocal },
                    forceDirectCall: true);
            }
        }
    }
}