using BenchmarkDotNet.Engines;
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

    /// <summary>Helper class that creates <see cref="BenchmarkAction"/> instances. </summary>
    public static partial class BenchmarkActionFactory
    {
        internal class BenchmarkActionVoid : BenchmarkActionBase
        {
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionVoid(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<Action>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = callback;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private static void OverheadStatic() { }
            private void OverheadInstance() { }

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }
        }

        internal unsafe class BenchmarkActionVoidPointer : BenchmarkActionBase
        {
            private delegate void* PointerFunc();

            private readonly PointerFunc callback;
            private readonly PointerFunc unrolledCallback;
            private readonly Consumer consumer = new ();

            public BenchmarkActionVoidPointer(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<PointerFunc>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private static void* OverheadStatic() => default;
            private void* OverheadInstance() => default;

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }

        internal unsafe class BenchmarkActionPointer<T> : BenchmarkActionBase
            where T : unmanaged
        {
            private delegate T* PointerFunc();

            private readonly PointerFunc callback;
            private readonly PointerFunc unrolledCallback;
            private readonly Consumer consumer = new ();

            public BenchmarkActionPointer(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<PointerFunc>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private static T* OverheadStatic() => default;
            private T* OverheadInstance() => default;

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }

        internal unsafe class BenchmarkActionByRef<T> : BenchmarkActionBase
        {
            private delegate ref T ByRefFunc();

            private readonly ByRefFunc callback;
            private readonly ByRefFunc unrolledCallback;
            private readonly Consumer consumer = new ();
            private static T overheadDefaultValueHolder;

            public BenchmarkActionByRef(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<ByRefFunc>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private static ref T OverheadStatic() => ref overheadDefaultValueHolder;
            private ref T OverheadInstance() => ref overheadDefaultValueHolder;

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }

        internal unsafe class BenchmarkActionByRefReadonly<T> : BenchmarkActionBase
        {
            private delegate ref readonly T ByRefReadonlyFunc();

            private readonly ByRefReadonlyFunc callback;
            private readonly ByRefReadonlyFunc unrolledCallback;
            private readonly Consumer consumer = new ();
            private static T overheadDefaultValueHolder;

            public BenchmarkActionByRefReadonly(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<ByRefReadonlyFunc>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private static ref readonly T OverheadStatic() => ref overheadDefaultValueHolder;
            private ref readonly T OverheadInstance() => ref overheadDefaultValueHolder;

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }

        internal class BenchmarkAction<T> : BenchmarkActionBase
        {
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private readonly Consumer consumer = new ();

            public BenchmarkAction(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<Func<T>>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private static T OverheadStatic() => default;

            private T OverheadInstance() => default;

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }

        internal class BenchmarkActionTask : BenchmarkActionBase
        {
            private readonly Func<Task> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionTask(object instance, MethodInfo method, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    startTaskCallback = CreateWorkload<Func<Task>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Overhead;
                }

                InvokeSingle = callback;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;

            }

            // must be kept in sync with VoidDeclarationsProvider.IdleImplementation
            private void Overhead() { }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => startTaskCallback.Invoke().GetAwaiter().GetResult();

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }
        }

        internal class BenchmarkActionTask<T> : BenchmarkActionBase
        {
            private readonly Func<Task<T>> startTaskCallback;
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private readonly Consumer consumer = new ();

            public BenchmarkActionTask(object instance, MethodInfo method, int unrollFactor)
            {
                bool isOverhead = method == null;
                if (!isOverhead)
                {
                    startTaskCallback = CreateWorkload<Func<Task<T>>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Overhead;
                }

                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private T Overhead() => default;

            // must be kept in sync with GenericTaskDeclarationsProvider.TargetMethodDelegate
            private T ExecuteBlocking() => startTaskCallback().GetAwaiter().GetResult();

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }

        internal class BenchmarkActionValueTask : BenchmarkActionBase
        {
            private readonly Func<ValueTask> startTaskCallback;
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionValueTask(object instance, MethodInfo method, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    startTaskCallback = CreateWorkload<Func<ValueTask>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Overhead;
                }

                InvokeSingle = callback;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;

            }

            // must be kept in sync with VoidDeclarationsProvider.IdleImplementation
            private void Overhead() { }

            // must be kept in sync with TaskDeclarationsProvider.TargetMethodDelegate
            private void ExecuteBlocking() => startTaskCallback.Invoke().GetAwaiter().GetResult();

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    unrolledCallback();
                }
            }
        }

        internal class BenchmarkActionValueTask<T> : BenchmarkActionBase
        {
            private readonly Func<ValueTask<T>> startTaskCallback;
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private readonly Consumer consumer = new ();

            public BenchmarkActionValueTask(object instance, MethodInfo method, int unrollFactor)
            {
                bool isOverhead = method == null;
                if (!isOverhead)
                {
                    startTaskCallback = CreateWorkload<Func<ValueTask<T>>>(instance, method);
                    callback = ExecuteBlocking;
                }
                else
                {
                    callback = Overhead;
                }

                InvokeSingle = InvokeSingleHardcoded;


                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeMultiple = InvokeMultipleHardcoded;
            }

            private T Overhead() => default;

            // must be kept in sync with GenericTaskDeclarationsProvider.TargetMethodDelegate
            private T ExecuteBlocking() => startTaskCallback().GetAwaiter().GetResult();

            private void InvokeSingleHardcoded() => consumer.Consume(callback());

            private void InvokeMultipleHardcoded(long repeatCount)
            {
                for (long i = 0; i < repeatCount; i++)
                {
                    consumer.Consume(unrolledCallback());
                }
            }
        }
    }
}