using BenchmarkDotNet.Helpers.Reflection.Emit;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;

partial class RunnableEmitter
{
    private sealed class SyncCoreEmitter(BuildPartition buildPartition, ModuleBuilder moduleBuilder, BenchmarkBuildInfo benchmark) : RunnableEmitter(buildPartition, moduleBuilder, benchmark)
    {
        protected override void EmitExtraGlobalCleanup(ILGenerator ilBuilder, LocalBuilder? thisLocal) { }

        protected override void EmitCoreImpl()
        {
            EmitAction(OverheadActionUnrollMethodName, overheadImplementationMethod, jobUnrollFactor);
            EmitAction(OverheadActionNoUnrollMethodName, overheadImplementationMethod, 1);
            EmitAction(WorkloadActionUnrollMethodName, Descriptor.WorkloadMethod, jobUnrollFactor);
            EmitAction(WorkloadActionNoUnrollMethodName, Descriptor.WorkloadMethod, 1);
        }

        private MethodBuilder EmitAction(string methodName, MethodInfo methodToCall, int unrollFactor)
        {
            /*
                .method private hidebysig 
	                instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan> OverheadActionNoUnroll (
		                int64 invokeCount,
		                class [Perfolizer]Perfolizer.Horology.IClock clock
	                ) cil managed flags(0200)
             */
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

            // init locals
            var argLocals = argFields.Select(a => ilBuilder.DeclareLocal(a.ArgLocalsType)).ToList();
            var startedClockLocal = ilBuilder.DeclareLocal(typeof(StartedClock));

            // load fields
            EmitLoadArgFieldsToLocals(ilBuilder, argLocals);

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
                for (int u = 0; u < unrollFactor; u++)
                {
                    /*
                        // InvokeOnceVoid();
                        IL_0008: ldarg.0
                        IL_0009: call instance void [BenchmarkDotNet.IntegrationTests]BenchmarkDotNet.IntegrationTests.InProcessEmitTest/BenchmarkAllCases::InvokeOnceVoid()
                     */
                    if (!methodToCall.IsStatic)
                    {
                        ilBuilder.Emit(OpCodes.Ldarg_0);
                    }
                    ilBuilder.EmitLdLocals(argLocals);
                    ilBuilder.Emit(OpCodes.Call, methodToCall);

                    if (methodToCall.ReturnType != typeof(void))
                    {
                        // IL_000b: pop
                        ilBuilder.Emit(OpCodes.Pop);
                    }
                }
            }
            ilBuilder.EmitLoopEndFromArgToZero(loopStartLabel, loopHeadLabel, invokeCountArg);

            /*
                // return new ValueTask<ClockSpan>(startedClock.GetElapsed());
	            IL_0034: ldloca.s 2
	            IL_0036: call instance valuetype [Perfolizer]Perfolizer.Horology.ClockSpan [Perfolizer]Perfolizer.Horology.StartedClock::GetElapsed()
	            IL_003b: newobj instance void valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<valuetype [Perfolizer]Perfolizer.Horology.ClockSpan>::.ctor(!0)
	            IL_0040: ret
             */
            ilBuilder.EmitLdloca(startedClockLocal);
            ilBuilder.Emit(OpCodes.Call, typeof(StartedClock).GetMethod(nameof(StartedClock.GetElapsed), BindingFlags.Public | BindingFlags.Instance)!);
            ilBuilder.Emit(OpCodes.Newobj, typeof(ValueTask<ClockSpan>).GetConstructor([typeof(ClockSpan)])!);
            ilBuilder.Emit(OpCodes.Ret);

            return actionMethodBuilder;
        }

        private void EmitLoadArgFieldsToLocals(ILGenerator ilBuilder, List<LocalBuilder> argLocals)
        {
            /*
                // bool _argField = __fieldsContainer.argField0;
                IL_0000: ldarg.0
                IL_0001: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
                IL_0006: ldfld bool BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::argField0
                IL_000b: stloc.0
                // int _argField2 = __fieldsContainer.argField1;
                IL_000c: ldarg.0
                IL_000d: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
                IL_0012: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::argField1
                IL_0017: stloc.1

                // -or-

                // ref bool _argField = ref __fieldsContainer.argField0;
                IL_0000: ldarg.0
                IL_0001: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
                IL_0006: ldflda bool BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::argField0
                IL_000b: stloc.0
                // ref int _argField2 = ref __fieldsContainer.argField1;
                IL_000c: ldarg.0
                IL_000d: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
                IL_0012: ldflda int32 BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::argField1
                IL_0017: stloc.1

                // -or- (ref struct arg call)

                // Span<int> arg = __fieldsContainer.argField0;
                IL_0000: ldarg.0
                IL_0001: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
                IL_0006: ldfld int32[] BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::argField0
                IL_000b: call valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<int32>::op_Implicit(!0[])
                IL_0010: stloc.0
             */
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