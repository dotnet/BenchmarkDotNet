using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;

partial class RunnableEmitter
{
    private void EmitSetupCleanup(string methodName, MethodInfo? methodToCall, bool isGlobalCleanup)
    {
        if (methodToCall?.ReturnType.IsAwaitable() == true)
        {
            EmitAsyncSetupCleanup(methodName, methodToCall, isGlobalCleanup);
        }
        else
        {
            EmitSyncSetupCleanup(methodName, methodToCall, isGlobalCleanup);
        }
    }

    private void EmitSyncSetupCleanup(string methodName, MethodInfo? methodToCall, bool isGlobalCleanup)
    {
        /*
            .method private hidebysig 
               instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask __GlobalSetup () cil managed flags(0200)
        */
        var methodBuilder = runnableBuilder
            .DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnParameter(typeof(ValueTask))
            )
            .SetAggressiveOptimizationImplementationFlag();
        var ilBuilder = methodBuilder.GetILGenerator();
        /*
            .locals init (
		        [0] valuetype [System.Runtime]System.Threading.Tasks.ValueTask
	        )
         */
        var valueTaskLocal = ilBuilder.DeclareLocal(typeof(ValueTask));
        if (isGlobalCleanup)
        {
            EmitExtraGlobalCleanup(ilBuilder, null);
        }
        if (methodToCall != null)
        {
            EmitNoArgsMethodCallPopReturn(methodBuilder, methodToCall, ilBuilder);
        }
        /*
            // return new ValueTask();
            IL_0000: ldloca.s 0
	        IL_0002: initobj [System.Runtime]System.Threading.Tasks.ValueTask
	        IL_0008: ldloc.0
	        IL_0009: ret
         */
        ilBuilder.EmitLdloca(valueTaskLocal);
        ilBuilder.Emit(OpCodes.Initobj, typeof(ValueTask));
        ilBuilder.EmitLdloc(valueTaskLocal);
        ilBuilder.Emit(OpCodes.Ret);
    }


    private void EmitAsyncSetupCleanup(string methodName, MethodInfo methodToCall, bool isGlobalCleanup)
        => EmitAsyncSingleCall(methodName, typeof(AsyncValueTaskMethodBuilder), methodToCall, isGlobalCleanup);

    // this.__fieldsContainer.workloadContinuerAndValueTaskSource?.Complete();
    protected abstract void EmitExtraGlobalCleanup(ILGenerator ilBuilder, LocalBuilder? thisLocal);
}