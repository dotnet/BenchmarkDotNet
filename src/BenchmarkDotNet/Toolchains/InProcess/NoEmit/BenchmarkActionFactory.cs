using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>Helper class that creates <see cref="BenchmarkAction"/> instances. </summary>
    public static partial class BenchmarkActionFactory
    {
        /// <summary>
        /// Dispatch method that creates <see cref="BenchmarkAction"/> using
        /// <paramref name="targetMethod"/> or <paramref name="fallbackIdleSignature"/> to find correct implementation.
        /// Either <paramref name="targetMethod"/> or <paramref name="fallbackIdleSignature"/> should be not <c>null</c>.
        /// </summary>
        private static BenchmarkAction CreateCore(
            object instance,
            MethodInfo? targetMethod,
            MethodInfo? fallbackIdleSignature,
            int unrollFactor)
        {
            PrepareInstanceAndResultType(instance, targetMethod, fallbackIdleSignature, out var resultInstance, out var resultType);

            if (resultType == typeof(void))
                return new BenchmarkActionVoid(resultInstance, targetMethod, unrollFactor);

            if (resultType == typeof(Task))
                return new BenchmarkActionTask(resultInstance, targetMethod, unrollFactor);

            if (resultType.GetTypeInfo().IsGenericType)
            {
                var genericType = resultType.GetGenericTypeDefinition();
                var argType = resultType.GenericTypeArguments[0];
                if (typeof(Task<>) == genericType)
                    return Create(
                        typeof(BenchmarkActionTask<>).MakeGenericType(argType),
                        resultInstance,
                        targetMethod,
                        unrollFactor);

                if (typeof(ValueTask<>).IsAssignableFrom(genericType))
                    return Create(
                        typeof(BenchmarkActionValueTask<>).MakeGenericType(argType),
                        resultInstance,
                        targetMethod,
                        unrollFactor);
            }

            if (targetMethod == null && resultType.GetTypeInfo().IsValueType)
                // for Idle: we return int because creating bigger ValueType could take longer than benchmarked method itself.
                resultType = typeof(int);

            return Create(
                typeof(BenchmarkAction<>).MakeGenericType(resultType),
                resultInstance,
                targetMethod,
                unrollFactor);
        }

        private static void PrepareInstanceAndResultType(
            object instance, MethodInfo targetMethod, MethodInfo fallbackIdleSignature,
            out object resultInstance, out Type resultType)
        {
            var signature = targetMethod ?? fallbackIdleSignature;
            if (signature == null)
                throw new ArgumentNullException(
                    nameof(fallbackIdleSignature),
                    $"Either {nameof(targetMethod)} or  {nameof(fallbackIdleSignature)} should be not null.");

            if (!signature.IsStatic && instance == null)
                throw new ArgumentNullException(
                    nameof(instance),
                    $"The {nameof(instance)} parameter should be not null as invocation method is instance method.");

            resultInstance = signature.IsStatic ? null : instance;
            resultType = signature.ReturnType;

            if (resultType == typeof(void))
            {
                // DONTTOUCH: async should be checked for target method
                // as fallbackIdleSignature used for result type detection only.
                bool isUsingAsyncKeyword = targetMethod?.HasAttribute<AsyncStateMachineAttribute>() ?? false;
                if (isUsingAsyncKeyword)
                    throw new NotSupportedException("Async void is not supported by design.");
            }
        }

        /// <summary>Helper to enforce .ctor signature.</summary>
        private static BenchmarkActionBase Create(Type actionType, object instance, MethodInfo method, int unrollFactor) =>
            (BenchmarkActionBase)Activator.CreateInstance(actionType, instance, method, unrollFactor);

        private static void FallbackMethod() { }
        private static readonly MethodInfo FallbackSignature = new Action(FallbackMethod).GetMethodInfo();
        private static readonly MethodInfo DummyMethod = typeof(DummyInstance).GetMethod(nameof(DummyInstance.Dummy));

        /// <summary>Creates run benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="unrollFactor">Unroll factor.</param>
        /// <returns>Run benchmark action.</returns>
        public static BenchmarkAction CreateWorkload(Descriptor descriptor, object instance, int unrollFactor) =>
            CreateCore(instance, descriptor.WorkloadMethod, null, unrollFactor);

        /// <summary>Creates idle benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="unrollFactor">Unroll factor.</param>
        /// <returns>Idle benchmark action.</returns>
        public static BenchmarkAction CreateOverhead(Descriptor descriptor, object instance, int unrollFactor) =>
            CreateCore(instance, null, descriptor.WorkloadMethod, unrollFactor);

        /// <summary>Creates global setup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <returns>Setup benchmark action.</returns>
        public static BenchmarkAction CreateGlobalSetup(Descriptor descriptor, object instance) =>
            CreateCore(instance, descriptor.GlobalSetupMethod, FallbackSignature, 1);

        /// <summary>Creates global cleanup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <returns>Cleanup benchmark action.</returns>
        public static BenchmarkAction CreateGlobalCleanup(Descriptor descriptor, object instance) =>
            CreateCore(instance, descriptor.GlobalCleanupMethod, FallbackSignature, 1);

        /// <summary>Creates global setup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <returns>Setup benchmark action.</returns>
        public static BenchmarkAction CreateIterationSetup(Descriptor descriptor, object instance) =>
            CreateCore(instance, descriptor.IterationSetupMethod, FallbackSignature, 1);

        /// <summary>Creates global cleanup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <returns>Cleanup benchmark action.</returns>
        public static BenchmarkAction CreateIterationCleanup(Descriptor descriptor, object instance) =>
            CreateCore(instance, descriptor.IterationCleanupMethod, FallbackSignature, 1);

        /// <summary>Creates a dummy benchmark action.</summary>
        /// <returns>Dummy benchmark action.</returns>
        public static BenchmarkAction CreateDummy() =>
            CreateCore(new DummyInstance(), DummyMethod, null, 1);
    }
}