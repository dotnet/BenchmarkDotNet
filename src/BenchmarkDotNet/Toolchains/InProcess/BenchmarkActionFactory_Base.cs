using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /*
        Design goals of the whole stuff:
        0. Reusable API to call Setup/Clean/Overhead/Workload actions with arbitrary return value and store the result.
            Supported ones are: void, T, Task, Task<T>, ValueTask<T>. No input args, same as for outofproc benchmarks.
        1. Overhead signature should match to the benchmark method signature (including static/instance modifier).
        2. Should work under .Net native. There's CodegenMode option to use Delegate.Combine instead of emitting the code.
        3. High data locality and no additional allocations / JIT where possible.
            This means NO closures allowed, no allocations but in .ctor and for LastCallResult boxing,
            all state should be stored explicitly as BenchmarkAction's fields.
        4. There can be multiple benchmark actions per single target instance (workload, globalSetup, globalCleanup methods),
            so target instantiation is not a responsibility of the benchmark action.
        5. Implementation should match to the code in BenchmarkProgram.txt.
            As example, this code emits loop unroll only, task waiting is implemented as a delegate call.
            Outofproc code uses TaskMethodInvoker.ExecuteBlocking callback for this.
     */

    // DONTTOUCH: Be VERY CAREFUL when changing the code.
    // Please, ensure that the implementation is in sync with content of BenchmarkProgram.txt

    /// <summary>Helper class that creates <see cref="BenchmarkAction"/> instances. </summary>
    public static partial class BenchmarkActionFactory
    {
        /// <summary>Base class that provides reusable API for final implementations.</summary>
        internal abstract class BenchmarkActionBase : BenchmarkAction
        {
            protected static TDelegate CreateWorkload<TDelegate>([CanBeNull] object targetInstance, MethodInfo workloadMethod)
            {
                if (workloadMethod.IsStatic)
                    return (TDelegate)(object)workloadMethod.CreateDelegate(typeof(TDelegate));

                return (TDelegate)(object)workloadMethod.CreateDelegate(typeof(TDelegate), targetInstance);
            }

            protected static TDelegate CreateWorkloadOrOverhead<TDelegate>(
                [CanBeNull] object targetInstance, [CanBeNull] MethodInfo workloadMethod,
                [NotNull] TDelegate overheadStaticCallback, [NotNull] TDelegate overheadInstanceCallback)
            {
                if (workloadMethod == null)
                    return targetInstance == null ? overheadStaticCallback : overheadInstanceCallback;

                if (workloadMethod.IsStatic)
                    return (TDelegate)(object)workloadMethod.CreateDelegate(typeof(TDelegate));

                return (TDelegate)(object)workloadMethod.CreateDelegate(typeof(TDelegate), targetInstance);
            }

            protected static bool UseFallbackCode(BenchmarkActionCodegen codegenMode, int unrollFactor) =>
                unrollFactor <= 1 || codegenMode == BenchmarkActionCodegen.DelegateCombine;

            protected static TDelegate Unroll<TDelegate>(TDelegate callback, int unrollFactor)
            {
                if (callback == null)
                    throw new ArgumentNullException(nameof(callback));

                if (unrollFactor <= 1)
                    return callback;

                return (TDelegate)(object)Delegate.Combine(
                    Enumerable.Repeat((Delegate)(object)callback, unrollFactor).ToArray());
            }

            private const BindingFlags GetFieldFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            protected static Action<long> EmitInvokeMultiple(
                BenchmarkActionBase instance,
                string callbackFieldName,
                string storeResultFieldName,
                int unrollFactor)
            {
                if (instance == null)
                    throw new ArgumentNullException(nameof(instance));

                if (callbackFieldName == null)
                    throw new ArgumentNullException(nameof(callbackFieldName));

                var instanceType = instance.GetType();
                var callbackField = GetCallbackField(instanceType, callbackFieldName);
                var callbackInvokeMethod = callbackField.FieldType.GetTypeInfo().GetMethod(nameof(Action.Invoke))
                    ?? throw new NullReferenceException($"{nameof(Action.Invoke)} not found");
                var storeResultField = GetStoreResultField(instanceType, storeResultFieldName, callbackInvokeMethod.ReturnType);

                // void InvokeMultipleEmitted(long x) // instance method associated with instanceType
                var m = new DynamicMethod("InvokeMultipleEmitted", typeof(void), new[] { instanceType, typeof(long) }, instanceType)
                {
                    InitLocals = true
                };

                EmitInvokeMultipleBody(m, callbackField, callbackInvokeMethod, storeResultField, unrollFactor);

                return (Action<long>)m.CreateDelegate(typeof(Action<long>), instance);
            }

            private static FieldInfo GetCallbackField(Type instanceType, string callbackFieldName)
            {
                var callbackField = instanceType.GetTypeInfo().GetField(callbackFieldName, GetFieldFlags);
                if (callbackField == null)
                    throw new ArgumentException($"Field {callbackFieldName} not found.", nameof(callbackFieldName));

                var callbackFieldType = callbackField.FieldType;
                if (callbackFieldType != typeof(Action) &&
                    (!callbackFieldType.GetTypeInfo().IsGenericType || callbackFieldType.GetGenericTypeDefinition() != typeof(Func<>)))
                    throw new ArgumentException(
                        $"Type of {callbackFieldName} field should be either Action or Func<T>.",
                        nameof(callbackFieldName));

                return callbackField;
            }

            private static FieldInfo GetStoreResultField(
                Type instanceType, string storeResultFieldName, Type expectedFieldType)
            {
                if (expectedFieldType == typeof(void) || storeResultFieldName == null)
                    return null;

                var storeResultField = instanceType.GetTypeInfo().GetField(storeResultFieldName, GetFieldFlags);

                if (expectedFieldType != storeResultField?.FieldType)
                    throw new ArgumentException(
                        $"Type of {storeResultFieldName} field should be equal to {expectedFieldType}.",
                        nameof(storeResultFieldName));

                return storeResultField;
            }

            private static void EmitInvokeMultipleBody(
                DynamicMethod dynamicMethod,
                FieldInfo callbackField,
                MethodInfo callbackInvokeMethod,
                FieldInfo storeResultField, int unrollFactor)
            {
                /*
                    // for long i = 0
                    IL_0000: ldc.i4.0
                    IL_0001: conv.i8
                    IL_0002: stloc.0

                    // jump to i < invokeCount
                    IL_0003: br IL_0041
                    // loop start (head: IL_0041)
                        IL_0005: ... // loop body

                        // i++;
                        IL_003d: ldc.i4.1
                        IL_003e: conv.i8
                        IL_003f: add
                        IL_0040: stloc.0

                        // i < invokeCount
                        IL_0041: ldloc.0
                        IL_0042: ldarg.1
                        IL_0043: blt IL_0005 // jump to loop start
                    // end loop

                    IL_0045: ret
                */

                bool noReturnValue = callbackInvokeMethod.ReturnType == typeof(void);
                bool hasStoreField = !noReturnValue && storeResultField != null;

                var g = dynamicMethod.GetILGenerator();
                g.DeclareLocal(typeof(long));

                var loopStart = g.DefineLabel();
                var loopCondition = g.DefineLabel();

                // for i = 0
                g.Emit(OpCodes.Ldc_I4_0);
                g.Emit(OpCodes.Conv_I8);
                g.Emit(OpCodes.Stloc_0);

                // jump to i < invokeCount
                g.Emit(OpCodes.Br, loopCondition);

                g.MarkLabel(loopStart);
                {
                    // loop body: callback(); unroll
                    for (int j = 0; j < unrollFactor; j++)
                    {
                        if (noReturnValue)
                        {
                            g.Emit(OpCodes.Ldarg_0); // this
                            g.Emit(OpCodes.Ldfld, callbackField);
                            g.Emit(OpCodes.Callvirt, callbackInvokeMethod);
                        }
                        else if (hasStoreField)
                        {
                            g.Emit(OpCodes.Ldarg_0); // this
                            g.Emit(OpCodes.Dup); // this (copy will be used to store the field)
                            g.Emit(OpCodes.Ldfld, callbackField);
                            g.Emit(OpCodes.Callvirt, callbackInvokeMethod);
                            g.Emit(OpCodes.Stfld, storeResultField);
                        }
                        else
                        {
                            g.Emit(OpCodes.Ldarg_0); // this
                            g.Emit(OpCodes.Ldfld, callbackField);
                            g.Emit(OpCodes.Callvirt, callbackInvokeMethod);
                            g.Emit(OpCodes.Pop); // ignore the return value
                        }
                    }

                    // i++;
                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Ldc_I4_1);
                    g.Emit(OpCodes.Conv_I8);
                    g.Emit(OpCodes.Add);
                    g.Emit(OpCodes.Stloc_0);

                    // i < invokeCount
                    g.MarkLabel(loopCondition);
                    {
                        g.Emit(OpCodes.Ldloc_0);
                        g.Emit(OpCodes.Ldarg_1);
                        g.Emit(OpCodes.Blt, loopStart); // jump to loop start
                    }
                }

                g.Emit(OpCodes.Ret);
            }
        }
    }
}