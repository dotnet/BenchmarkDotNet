using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Helpers.Reflection.Emit
{
    internal static class IlGeneratorCallExtensions
    {
        public static LocalBuilder DeclareOptionalLocalForInstanceCall(
            this ILGenerator ilBuilder,
            Type localType,
            MethodInfo methodToCall)
        {
            if (methodToCall.DeclaringType == null)
                throw new ArgumentException($"The {nameof(methodToCall)} should have non-null {nameof(methodToCall.DeclaringType)}.");

            if (methodToCall.IsStatic)
                return null;

            if (!methodToCall.DeclaringType.IsAssignableFrom(localType))
                throw new ArgumentException($"{methodToCall.DeclaringType} is not assignable from {localType}.");

            return localType.IsValueType && localType != typeof(void)
                ? ilBuilder.DeclareLocal(localType)
                : null;
        }

        public static void EmitStaticCall(
            this ILGenerator ilBuilder,
            MethodInfo methodToCall,
            params LocalBuilder[] argLocals)
        {
            if (!methodToCall.IsStatic)
                throw new ArgumentException($"Method {methodToCall} should be static method.");

            EmitCallCore(ilBuilder, null, methodToCall, argLocals);
        }

        public static void EmitInstanceCallThisValueOnStack(
            this ILGenerator ilBuilder,
            LocalBuilder optionalLocalThis,
            MethodInfo methodToCall,
            params LocalBuilder[] argLocals) =>
            EmitInstanceCallThisValueOnStack(ilBuilder, optionalLocalThis, methodToCall, argLocals, false);

        public static void EmitInstanceCallThisValueOnStack(
            this ILGenerator ilBuilder,
            LocalBuilder optionalLocalThis,
            MethodInfo methodToCall,
            IEnumerable<LocalBuilder> argLocals,
            bool forceDirectCall = false)
        {
            if (methodToCall.DeclaringType == null)
                throw new ArgumentException($"The {nameof(methodToCall)} should have non-null {nameof(methodToCall.DeclaringType)}.");

            if (methodToCall.IsStatic)
                throw new ArgumentException($"Method {methodToCall} should be instance method.");

            /*
                IL_0005: stloc.s 11
                ...
                IL_0007: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [mscorlib]System.Threading.Tasks.Task`1<int32>::GetAwaiter()
                --or-
                IL_000d: ldloca.s 11
                IL_000f: call instance !0 valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::GetResult()
            */
            if (methodToCall.DeclaringType.IsValueType)
            {
                if (optionalLocalThis == null)
                    throw new ArgumentNullException(nameof(optionalLocalThis));

                ilBuilder.EmitStloc(optionalLocalThis);
                EmitCallCore(ilBuilder, optionalLocalThis, methodToCall, argLocals, forceDirectCall);
            }
            else
            {
                EmitCallCore(ilBuilder, null, methodToCall, argLocals, forceDirectCall);
            }
        }

        private static void EmitCallCore(
            this ILGenerator ilBuilder,
            LocalBuilder optionalLocalThis,
            MethodInfo methodToCall,
            IEnumerable<LocalBuilder> argLocals,
            bool forceDirectCall = false)
        {
            if (methodToCall.DeclaringType == null)
                throw new ArgumentException($"The {nameof(methodToCall)} should have non-null {nameof(methodToCall.DeclaringType)}.");

            /*
                IL_0000: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::get_CompletedTask()
                -or-
                IL_0007: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [mscorlib]System.Threading.Tasks.Task`1<int32>::GetAwaiter()
                --or-
                IL_000d: ldloca.s 0
                IL_000f: call instance !0 valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::GetResult()
            */
            if (methodToCall.IsStatic)
            {
                if (optionalLocalThis != null)
                    throw new ArgumentException($"The {nameof(optionalLocalThis)} should be null for static calls", nameof(optionalLocalThis));

                ilBuilder.EmitLdLocals(argLocals);
                ilBuilder.Emit(OpCodes.Call, methodToCall);
            }
            else if (methodToCall.DeclaringType.IsValueType)
            {
                if (optionalLocalThis == null)
                    throw new ArgumentNullException(nameof(optionalLocalThis));

                ilBuilder.EmitLdloca(optionalLocalThis);
                ilBuilder.EmitLdLocals(argLocals);
                ilBuilder.Emit(OpCodes.Call, methodToCall);
            }
            else
            {
                if (optionalLocalThis != null)
                    ilBuilder.EmitLdloc(optionalLocalThis);

                ilBuilder.EmitLdLocals(argLocals);

                if (forceDirectCall)
                    ilBuilder.Emit(OpCodes.Call, methodToCall);
                else
                    ilBuilder.Emit(OpCodes.Callvirt, methodToCall);
            }
        }
    }
}