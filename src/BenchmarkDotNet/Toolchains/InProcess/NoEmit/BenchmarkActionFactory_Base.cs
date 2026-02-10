using System;
using System.Reflection;

#nullable enable

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /*
        Design goals of the whole stuff:
        0. Reusable API to call Setup/Clean/Overhead/Workload actions with arbitrary return value and store the result.
            Supported ones are: void, T, Task, Task<T>, ValueTask<T>. No input args.
        1. Overhead signature should match to the benchmark method signature (including static/instance modifier).
        2. Should work under .Net native. Uses Delegate.Combine instead of emitting the code.
        3. High data locality and no additional allocations / JIT where possible.
            This means NO closures allowed, no allocations but in .ctor and for LastCallResult boxing,
            all state should be stored explicitly as BenchmarkAction's fields.
        4. There can be multiple benchmark actions per single target instance (workload, globalSetup, globalCleanup methods),
            so target instantiation is not a responsibility of the benchmark action.
        5. Implementation should match to the code in BenchmarkProgram.txt.
     */

    // DONTTOUCH: Be VERY CAREFUL when changing the code.
    // Please, ensure that the implementation is in sync with content of BenchmarkProgram.txt
    internal static partial class BenchmarkActionFactory
    {
        /// <summary>Base class that provides reusable API for final implementations.</summary>
        internal abstract class BenchmarkActionBase : BenchmarkAction
        {
            protected static TDelegate CreateWorkload<TDelegate>(object? targetInstance, MethodInfo workloadMethod)
            {
                if (workloadMethod.IsStatic)
                    return (TDelegate)(object)workloadMethod.CreateDelegate(typeof(TDelegate));

                return (TDelegate)(object)workloadMethod.CreateDelegate(typeof(TDelegate), targetInstance);
            }

            protected Action CreateWorkloadOrOverhead(object? instance, MethodInfo? method)
            {
                if (method == null)
                {
                    return instance == null ? OverheadStatic : OverheadInstance;
                }
                return method.IsStatic
                    ? (Action) method.CreateDelegate(typeof(Action))
                    : (Action) method.CreateDelegate(typeof(Action), instance);
            }

            protected static TDelegate Unroll<TDelegate>(TDelegate callback, int unrollFactor)
                 where TDelegate : notnull, Delegate
            {
                if (unrollFactor <= 1)
                    return callback;

                var delegates = new Delegate[unrollFactor];
                for (int i = 0; i < unrollFactor; i++)
                    delegates[i] = callback;

                return (TDelegate)Delegate.Combine(delegates)!;
            }

            // must be kept in sync with VoidDeclarationsProvider.IdleImplementation
            private static void OverheadStatic() { }

            private void OverheadInstance() { }
        }
    }
}