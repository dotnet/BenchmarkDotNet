using BenchmarkDotNet.Helpers.Reflection.Emit;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;

partial class RunnableEmitter
{
    // MethodImplAttributes.Async was added in .Net 10.
    private const MethodImplAttributes AsyncMethodImplAttribute = (MethodImplAttributes) 8192;

    // Looked up at runtime because the frameworks BenchmarkDotNet targets either lack it or mark it experimental.
    private static readonly Lazy<Type> AsyncHelpersType = new(() =>
        typeof(object).Assembly.GetType("System.Runtime.CompilerServices.AsyncHelpers", throwOnError: true)!);

    /// <summary>
    /// The csproj toolchains compile the runnable with the benchmark project's compiler features,
    /// so Roslyn makes a method awaiting a runtime-async method runtime-async too.
    /// </summary>
    private static bool IsRuntimeAsync(MethodInfo method) => (method.MethodImplementationFlags & AsyncMethodImplAttribute) != 0;

    private MethodBuilder DefineRuntimeAsyncMethod(string methodName, Type returnType)
    {
        var methodBuilder = runnableBuilder
            .DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnParameter(returnType)
            )
            .SetAggressiveOptimizationImplementationFlag();
        methodBuilder.SetImplementationFlags(methodBuilder.GetMethodImplementationFlags() | AsyncMethodImplAttribute);
        return methodBuilder;
    }

    /// <summary>
    /// Awaits the value on the stack returned by a runtime-async method, leaving its result (if any) on the stack.
    /// </summary>
    /// <returns>The result type, <see cref="void"/> when nothing is left on the stack.</returns>
    private static Type EmitRuntimeAsyncAwait(ILGenerator ilBuilder, Type awaitableType)
    {
        /*
            // await task;
            IL_0006: call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
         */
        var awaitMethod = GetAsyncHelpersAwaitMethod(awaitableType);
        ilBuilder.Emit(OpCodes.Call, awaitMethod);
        return awaitMethod.ReturnType;
    }

    // AsyncHelpers.Await has an overload for each type a runtime-async method can return, which Roslyn calls directly.
    private static MethodInfo GetAsyncHelpersAwaitMethod(Type awaitableType)
    {
        foreach (var method in AsyncHelpersType.Value.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "Await" || method.GetParameters() is not [var parameter])
                continue;

            var parameterType = parameter.ParameterType;
            if (!method.IsGenericMethodDefinition)
            {
                if (parameterType == awaitableType)
                    return method;
            }
            else if (awaitableType.IsGenericType
                && parameterType.IsGenericType
                && parameterType.GetGenericTypeDefinition() == awaitableType.GetGenericTypeDefinition())
            {
                return method.MakeGenericMethod(awaitableType.GetGenericArguments());
            }
        }
        throw new NotSupportedException($"AsyncHelpers has no Await overload for {awaitableType}.");
    }

    /*
        private async ValueTask GlobalSetup()
        {
            await base.GlobalSetup();
        }
     */
    private void EmitRuntimeAsyncSetupCleanup(string methodName, MethodInfo methodToCall, SetupCleanupKind kind)
    {
        /*
            .method private hidebysig
               instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask GlobalSetup () cil managed flags(2200)
        */
        var methodBuilder = DefineRuntimeAsyncMethod(methodName, typeof(ValueTask));
        var ilBuilder = methodBuilder.GetILGenerator();

        if (kind == SetupCleanupKind.GlobalCleanup)
        {
            EmitExtraGlobalCleanup(ilBuilder, null);
        }
        /*
            // await base.GlobalSetup();
            IL_0000: ldarg.0
            IL_0001: call instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask [BenchmarkDotNet.IntegrationTests]BenchmarkDotNet.IntegrationTests.RuntimeAsync.RuntimeAsyncTaskCaseBenchmark::GlobalSetup()
            IL_0006: call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(valuetype [System.Runtime]System.Threading.Tasks.ValueTask)
         */
        if (!methodToCall.IsStatic)
        {
            ilBuilder.Emit(OpCodes.Ldarg_0);
        }
        ilBuilder.Emit(OpCodes.Call, methodToCall);
        if (EmitRuntimeAsyncAwait(ilBuilder, methodToCall.ReturnType) != typeof(void))
        {
            ilBuilder.Emit(OpCodes.Pop);
        }
        if (kind == SetupCleanupKind.GlobalSetup)
        {
            EmitExtraGlobalSetup(ilBuilder, null);
        }
        /*
            IL_0021: ret
         */
        ilBuilder.Emit(OpCodes.Ret);
    }
}
