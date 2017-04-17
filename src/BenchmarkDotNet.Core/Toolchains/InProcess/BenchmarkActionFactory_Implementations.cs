using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /*
        Design goals of the whole stuff: check the comments for BenchmarkActionBase.
     */

    // DONTTOUCH: Be VERY CAREFUL when changing the code.
    // Please, ensure that the implementation is in sync with content of BenchmarkProgram.txt

    /// <summary>Helper class that creates <see cref="BenchmarkAction"/> instances. </summary>
    public static partial class BenchmarkActionFactory
    {
        internal class BenchmarkActionVoid : BenchmarkActionBase
        {
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionVoid(object instance, MethodInfo method, BenchmarkActionCodegen codegenMode, int unrollFactor)
            {
                callback = CreateMainOrIdle<Action>(instance, method, IdleStatic, IdleInstance);
                InvokeSingle = callback;

                if (UseFallbackCode(codegenMode, unrollFactor))
                {
                    unrolledCallback = Unroll(callback, unrollFactor);
                    InvokeMultiple = InvokeMultipleHardcoded;
                }
                else
                {
                    InvokeMultiple = EmitInvokeMultiple(this, nameof(callback), null, unrollFactor);
                }
            }

            private static void IdleStatic() { }
            private void IdleInstance() { }

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                    unrolledCallback();
            }
        }

        internal class BenchmarkAction<T> : BenchmarkActionBase
        {
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private T result;

            public BenchmarkAction(object instance, MethodInfo method, BenchmarkActionCodegen codegenMode, int unrollFactor)
            {
                callback = CreateMainOrIdle<Func<T>>(instance, method, IdleStatic, IdleInstance);
                InvokeSingle = InvokeSingleHardcoded;

                if (UseFallbackCode(codegenMode, unrollFactor))
                {
                    unrolledCallback = Unroll(callback, unrollFactor);
                    InvokeMultiple = InvokeMultipleHardcoded;
                }
                else
                {
                    InvokeMultiple = EmitInvokeMultiple(this, nameof(callback), nameof(result), unrollFactor);
                }
            }

            private static T IdleStatic() => default(T);
            private T IdleInstance() => default(T);

            private void InvokeSingleHardcoded() => result = callback();

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                    result = unrolledCallback();
            }

            public override object LastRunResult => result;
        }

        internal class BenchmarkActionTask : BenchmarkActionBase
        {
            private readonly Func<Task> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionTask(object instance, MethodInfo method, BenchmarkActionCodegen codegenMode, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    startTaskCallback = CreateMain<Func<Task>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Idle;
                }

                InvokeSingle = callback;

                if (UseFallbackCode(codegenMode, unrollFactor))
                {
                    unrolledCallback = Unroll(callback, unrollFactor);
                    InvokeMultiple = InvokeMultipleHardcoded;
                }
                else
                {
                    InvokeMultiple = EmitInvokeMultiple(this, nameof(callback), null, unrollFactor);
                }
            }

            // must be kept in sync with VoidDeclarationsProvider.IdleImplementation
            private void Idle() { }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => startTaskCallback.Invoke().GetAwaiter().GetResult();

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                    unrolledCallback();
            }
        }

        internal class BenchmarkActionTask<T> : BenchmarkActionBase
        {
            private readonly Func<Task<T>> startTaskCallback;
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private T result;

            public BenchmarkActionTask(object instance, MethodInfo method, BenchmarkActionCodegen codegenMode, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    startTaskCallback = CreateMain<Func<Task<T>>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Idle;
                }

                InvokeSingle = InvokeSingleHardcoded;

                if (UseFallbackCode(codegenMode, unrollFactor))
                {
                    unrolledCallback = Unroll(callback, unrollFactor);
                    InvokeMultiple = InvokeMultipleHardcoded;
                }
                else
                {
                    InvokeMultiple = EmitInvokeMultiple(this, nameof(callback), nameof(result), unrollFactor);
                }
            }

            private T Idle() => default(T);

            // must be kept in sync with GenericTaskDeclarationsProvider.TargetMethodDelegate
            private T ExecuteBlocking() => startTaskCallback().GetAwaiter().GetResult();

            private void InvokeSingleHardcoded() => result = callback();

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                    result = unrolledCallback();
            }

            public override object LastRunResult => result;
        }

        internal class BenchmarkActionValueTask<T> : BenchmarkActionBase
        {
            private readonly Func<ValueTask<T>> startTaskCallback;
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private T result;

            public BenchmarkActionValueTask(object instance, MethodInfo method, BenchmarkActionCodegen codegenMode, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    startTaskCallback = CreateMain<Func<ValueTask<T>>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Idle;
                }

                InvokeSingle = InvokeSingleHardcoded;

                if (UseFallbackCode(codegenMode, unrollFactor))
                {
                    unrolledCallback = Unroll(callback, unrollFactor);
                    InvokeMultiple = InvokeMultipleHardcoded;
                }
                else
                {
                    InvokeMultiple = EmitInvokeMultiple(this, nameof(callback), nameof(result), unrollFactor);
                }
            }

            private T Idle() => default(T);

            // must be kept in sync with GenericTaskDeclarationsProvider.TargetMethodDelegate
            private T ExecuteBlocking() => startTaskCallback().GetAwaiter().GetResult();

            private void InvokeSingleHardcoded() => result = callback();

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                    result = unrolledCallback();
            }

            public override object LastRunResult => result;
        }
    }
}