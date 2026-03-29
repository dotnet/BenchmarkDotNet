using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal static class BenchmarkActionFactory
{
    private static IBenchmarkAction CreateCore(IBenchmarkActionFactory? factory, object instance, MethodInfo targetMethod, int unrollFactor)
    {
        if (factory?.TryCreate(instance, targetMethod, unrollFactor, out var benchmarkAction) == true)
        {
            return benchmarkAction;
        }

        PrepareInstanceAndResultType(instance, targetMethod, out var resultInstance, out var resultType);

        if (resultType == typeof(void))
            return new BenchmarkActionVoid(resultInstance, targetMethod, unrollFactor);

        if (resultType == typeof(void*))
            return new BenchmarkActionVoidPointer(resultInstance, targetMethod, unrollFactor);

        if (resultType.IsByRef)
        {
            var returnParameter = targetMethod.ReturnParameter;
            // IsReadOnlyAttribute is not part of netstandard2.0, so we need to check the attribute name as usual.
            if (returnParameter.GetCustomAttributes().Any(attribute => attribute.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"))
                return Create(
                    typeof(BenchmarkActionByRefReadonly<>).MakeGenericType(resultType.GetElementType()!),
                    resultInstance,
                    targetMethod,
                    unrollFactor);

            return Create(
                typeof(BenchmarkActionByRef<>).MakeGenericType(resultType.GetElementType()!),
                resultInstance,
                targetMethod,
                unrollFactor);
        }

        if (resultType == typeof(Task))
            return new BenchmarkActionTask(resultInstance, targetMethod, unrollFactor);

        if (resultType == typeof(ValueTask))
            return new BenchmarkActionValueTask(resultInstance, targetMethod, unrollFactor);

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

        if (resultType.IsAwaitable())
        {
            throw new NotSupportedException($"Default {nameof(BenchmarkActionFactory)} does not support returning awaitable types except (Value)Task(<T>).");
        }

        return Create(
            typeof(BenchmarkAction<>).MakeGenericType(resultType),
            resultInstance,
            targetMethod,
            unrollFactor);
    }

    private static void PrepareInstanceAndResultType(object instance, MethodInfo targetMethod, out object? resultInstance, out Type resultType)
    {
        resultInstance = targetMethod.IsStatic ? null : instance;
        resultType = targetMethod.ReturnType;

        if (resultType == typeof(void))
        {
            // DONTTOUCH: async should be checked for target method
            // as fallbackIdleSignature used for result type detection only.
            bool isUsingAsyncKeyword = targetMethod?.HasAttribute<AsyncStateMachineAttribute>() ?? false;
            if (isUsingAsyncKeyword)
                throw new NotSupportedException("Async void is not supported by design.");
        }
        else if (resultType.IsPointer && resultType != typeof(void*))
        {
            throw new NotSupportedException($"Default {nameof(BenchmarkActionFactory)} only supports void* return, not T*");
        }
    }

    /// <summary>Helper to enforce .ctor signature.</summary>
    private static BenchmarkActionBase Create(Type actionType, object? instance, MethodInfo method, int unrollFactor) =>
        (BenchmarkActionBase)Activator.CreateInstance(actionType, instance, method, unrollFactor)!;

    private static readonly MethodInfo FallbackSignature = new Action(BenchmarkActionBase.OverheadStatic).GetMethodInfo();

    public static IBenchmarkAction CreateWorkload(IBenchmarkActionFactory? factory, Descriptor descriptor, object instance, int unrollFactor) =>
        CreateCore(factory, instance, descriptor.WorkloadMethod, unrollFactor);

    public static IBenchmarkAction CreateOverhead(IBenchmarkActionFactory? factory, Descriptor descriptor, object instance, int unrollFactor) =>
        CreateCore(factory, instance, FallbackSignature, unrollFactor);

    public static IBenchmarkAction CreateGlobalSetup(IBenchmarkActionFactory? factory, Descriptor descriptor, object instance) =>
        CreateCore(factory, instance, descriptor.GlobalSetupMethod ?? FallbackSignature, 1);

    public static IBenchmarkAction CreateGlobalCleanup(IBenchmarkActionFactory? factory, Descriptor descriptor, object instance) =>
        CreateCore(factory, instance, descriptor.GlobalCleanupMethod ?? FallbackSignature, 1);

    public static IBenchmarkAction CreateIterationSetup(IBenchmarkActionFactory? factory, Descriptor descriptor, object instance) =>
        CreateCore(factory, instance, descriptor.IterationSetupMethod ?? FallbackSignature, 1);

    public static IBenchmarkAction CreateIterationCleanup(IBenchmarkActionFactory? factory, Descriptor descriptor, object instance) =>
        CreateCore(factory, instance, descriptor.IterationCleanupMethod ?? FallbackSignature, 1);
}