using BenchmarkDotNet.Portability;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /*
        Design goals of the whole stuff: check the comments for BenchmarkActionBase.
     */

    // DONTTOUCH: Be VERY CAREFUL when changing the code.
    // Please, ensure that the implementation is in sync with content of BenchmarkProgram.txt
    internal static partial class BenchmarkActionFactory
    {
        internal sealed class BenchmarkActionVoid : BenchmarkActionBase
        {
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionVoid(object? instance, MethodInfo? method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead(instance, method);
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = callback;
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal unsafe class BenchmarkActionVoidPointer : BenchmarkActionBase
        {
            private delegate void* PointerFunc();

            private readonly PointerFunc callback;
            private readonly PointerFunc unrolledCallback;

            public BenchmarkActionVoidPointer(object? instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkload<PointerFunc>(instance, method);
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = () => callback();
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal unsafe class BenchmarkActionByRef<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            private delegate ref T ByRefFunc();

            private readonly ByRefFunc callback;
            private readonly ByRefFunc unrolledCallback;

            public BenchmarkActionByRef(object? instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkload<ByRefFunc>(instance, method);
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = () => callback();
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal unsafe class BenchmarkActionByRefReadonly<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            private delegate ref readonly T ByRefReadonlyFunc();

            private readonly ByRefReadonlyFunc callback;
            private readonly ByRefReadonlyFunc unrolledCallback;

            public BenchmarkActionByRefReadonly(object? instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkload<ByRefReadonlyFunc>(instance, method);
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = () => callback();
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal class BenchmarkAction<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;

            public BenchmarkAction(object? instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkload<Func<T>>(instance, method);
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = () => callback();
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal class BenchmarkActionTask : BenchmarkActionBase
        {
            private readonly Func<Task> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionTask(object? instance, MethodInfo method, int unrollFactor)
            {
                if (method == null)
                {
                    startTaskCallback = default!;
                    callback = CreateWorkloadOrOverhead(instance, method);
                }
                else
                {
                    startTaskCallback = CreateWorkload<Func<Task>>(instance, method);
                    callback = ExecuteBlocking;
                }
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = callback;
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => Helpers.AwaitHelper.GetResult(startTaskCallback.Invoke());

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal class BenchmarkActionTask<T> : BenchmarkActionBase
        {
            private readonly Func<Task<T>> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionTask(object? instance, MethodInfo method, int unrollFactor)
            {
                if (method == null)
                {
                    startTaskCallback = default!;
                    callback = CreateWorkloadOrOverhead(instance, method);
                }
                else
                {
                    startTaskCallback = CreateWorkload<Func<Task<T>>>(instance, method);
                    callback = ExecuteBlocking;
                }
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = callback;
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => Helpers.AwaitHelper.GetResult(startTaskCallback.Invoke());

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal class BenchmarkActionValueTask : BenchmarkActionBase
        {
            private readonly Func<ValueTask> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionValueTask(object? instance, MethodInfo method, int unrollFactor)
            {
                if (method == null)
                {
                    startTaskCallback = default!;
                    callback = CreateWorkloadOrOverhead(instance, method);
                }
                else
                {
                    startTaskCallback = CreateWorkload<Func<ValueTask>>(instance, method);
                    callback = ExecuteBlocking;
                }
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = callback;
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => Helpers.AwaitHelper.GetResult(startTaskCallback.Invoke());

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }

        internal class BenchmarkActionValueTask<T> : BenchmarkActionBase
        {
            private readonly Func<ValueTask<T>> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionValueTask(object? instance, MethodInfo method, int unrollFactor)
            {
                if (method == null)
                {
                    startTaskCallback = default!;
                    callback = CreateWorkloadOrOverhead(instance, method);
                }
                else
                {
                    startTaskCallback = CreateWorkload<Func<ValueTask<T>>>(instance, method);
                    callback = ExecuteBlocking;
                }
                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeSingle = callback;
                InvokeUnroll = WorkloadActionUnroll;
                InvokeNoUnroll = WorkloadActionNoUnroll;
            }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => Helpers.AwaitHelper.GetResult(startTaskCallback.Invoke());

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            private void WorkloadActionNoUnroll(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    callback();
                }
            }
        }
    }
}