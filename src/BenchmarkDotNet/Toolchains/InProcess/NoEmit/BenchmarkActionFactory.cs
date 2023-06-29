using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

using JetBrains.Annotations;

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
            int unrollFactor,
            IConfig config)
        {
            PrepareInstanceAndResultType(instance, targetMethod, fallbackIdleSignature, out var resultInstance, out var resultType);

            if (resultType == typeof(void))
                return new BenchmarkActionVoid(resultInstance, targetMethod, unrollFactor);

            if (config.GetIsAwaitable(resultType, out var asyncConsumerType))
            {
                return CreateBenchmarkActionAwaitable(asyncConsumerType, resultType, resultInstance, targetMethod, unrollFactor);
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

        private static BenchmarkActionBase CreateBenchmarkActionAwaitable(Type asyncConsumerType, Type awaitableType, object instance, MethodInfo method, int unrollFactor)
        {
            var asyncConsumerInterfaceType = asyncConsumerType.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAsyncVoidConsumer<,>));
            bool isVoidConsumer = asyncConsumerInterfaceType?.GetGenericArguments()[0] == awaitableType;
            if (!isVoidConsumer)
            {
                asyncConsumerInterfaceType = asyncConsumerType.GetInterfaces().First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAsyncResultConsumer<,,>));
            }

            Type[] genericArguments = asyncConsumerInterfaceType.GetGenericArguments();
            Type awaiterType = genericArguments[1];

            if (isVoidConsumer)
            {
                return (BenchmarkActionBase) Activator.CreateInstance(
                    typeof(BenchmarkActionAwaitable<,,>).MakeGenericType(asyncConsumerType, awaitableType, awaiterType),
                    instance,
                    method,
                    unrollFactor);
            }
            return (BenchmarkActionBase) Activator.CreateInstance(
                typeof(BenchmarkActionAwaitable<,,,>).MakeGenericType(asyncConsumerType, awaitableType, awaiterType, genericArguments[2]),
                instance,
                method,
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

        internal static int GetUnrollFactor(BenchmarkCase benchmarkCase)
        {
            if (benchmarkCase.Config.GetIsAwaitable(benchmarkCase.Descriptor.WorkloadMethod.ReturnType, out _))
            {
                benchmarkCase.ForceUnrollFactorForAsync();
            }
            return benchmarkCase.Job.ResolveValue(Jobs.RunMode.UnrollFactorCharacteristic, Environments.EnvironmentResolver.Instance);
        }

        /// <summary>Creates run benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="unrollFactor">Unroll factor.</param>
        /// <param name="config">Config.</param>
        /// <returns>Run benchmark action.</returns>
        public static BenchmarkAction CreateWorkload(Descriptor descriptor, object instance, int unrollFactor, IConfig config) =>
            CreateCore(instance, descriptor.WorkloadMethod, null, unrollFactor, config);

        /// <summary>Creates idle benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="unrollFactor">Unroll factor.</param>
        /// <param name="config">Config.</param>
        /// <returns>Idle benchmark action.</returns>
        public static BenchmarkAction CreateOverhead(Descriptor descriptor, object instance, int unrollFactor, IConfig config) =>
            CreateCore(instance, null, descriptor.WorkloadMethod, unrollFactor, config);

        /// <summary>Creates global setup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="config">Config.</param>
        /// <returns>Setup benchmark action.</returns>
        public static BenchmarkAction CreateGlobalSetup(Descriptor descriptor, object instance, IConfig config) =>
            CreateCore(instance, descriptor.GlobalSetupMethod, FallbackSignature, 1, config);

        /// <summary>Creates global cleanup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="config">Config.</param>
        /// <returns>Cleanup benchmark action.</returns>
        public static BenchmarkAction CreateGlobalCleanup(Descriptor descriptor, object instance, IConfig config) =>
            CreateCore(instance, descriptor.GlobalCleanupMethod, FallbackSignature, 1, config);

        /// <summary>Creates global setup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="config">Config.</param>
        /// <returns>Setup benchmark action.</returns>
        public static BenchmarkAction CreateIterationSetup(Descriptor descriptor, object instance, IConfig config) =>
            CreateCore(instance, descriptor.IterationSetupMethod, FallbackSignature, 1, config);

        /// <summary>Creates global cleanup benchmark action.</summary>
        /// <param name="descriptor">Descriptor info.</param>
        /// <param name="instance">Instance of target.</param>
        /// <param name="config">Config.</param>
        /// <returns>Cleanup benchmark action.</returns>
        public static BenchmarkAction CreateIterationCleanup(Descriptor descriptor, object instance, IConfig config) =>
            CreateCore(instance, descriptor.IterationCleanupMethod, FallbackSignature, 1, config);

        /// <summary>Creates a dummy benchmark action.</summary>
        /// <returns>Dummy benchmark action.</returns>
        public static BenchmarkAction CreateDummy() =>
            CreateCore(new DummyInstance(), DummyMethod, null, 1, DefaultConfig.Instance);
    }
}