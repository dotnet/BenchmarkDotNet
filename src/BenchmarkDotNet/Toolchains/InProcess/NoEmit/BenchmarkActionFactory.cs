using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal static partial class BenchmarkActionFactory
{
    /// <summary>
    /// Dispatch method that creates <see cref="BenchmarkAction"/> using
    /// <paramref name="targetMethod"/> or <paramref name="fallbackIdleSignature"/> to find correct implementation.
    /// Either <paramref name="targetMethod"/> or <paramref name="fallbackIdleSignature"/> should be not <c>null</c>.
    /// </summary>
    private static BenchmarkAction CreateCore(object instance, MethodInfo? targetMethod, MethodInfo? fallbackIdleSignature, int unrollFactor)
    {
        PrepareInstanceAndResultType(instance, targetMethod, fallbackIdleSignature, out var resultInstance, out var resultType);

        if (resultType == typeof(void))
            return new BenchmarkActionVoid(resultInstance, targetMethod, unrollFactor);

        // targetMethod must not be null here. Because it's checked by PrepareInstanceAndResultType.
        // Following null check is added to suppress nullable annotation errors.
        if (targetMethod == null)
            throw new ArgumentNullException(nameof(targetMethod));

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
            throw new NotSupportedException($"{nameof(InProcessNoEmitToolchain)} does not support returning awaitable types except (Value)Task(<T>).");
        }

        return Create(
            typeof(BenchmarkAction<>).MakeGenericType(resultType),
            resultInstance,
            targetMethod,
            unrollFactor);
    }

    private static void PrepareInstanceAndResultType(object instance, MethodInfo? targetMethod, MethodInfo? fallbackIdleSignature, out object? resultInstance, out Type resultType)
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
        else if (resultType.IsPointer && resultType != typeof(void*))
        {
            throw new NotSupportedException("InProcessNoEmitToolchain only supports void* return, not T*");
        }
    }

    /// <summary>Helper to enforce .ctor signature.</summary>
    private static BenchmarkActionBase Create(Type actionType, object? instance, MethodInfo method, int unrollFactor) =>
        (BenchmarkActionBase)Activator.CreateInstance(actionType, instance, method, unrollFactor)!;

    private static void FallbackMethod() { }
    private static readonly MethodInfo FallbackSignature = new Action(FallbackMethod).GetMethodInfo();

    public static BenchmarkAction CreateWorkload(Descriptor descriptor, object instance, int unrollFactor) =>
        CreateCore(instance, descriptor.WorkloadMethod, null, unrollFactor);

    public static BenchmarkAction CreateOverhead(Descriptor descriptor, object instance, int unrollFactor) =>
        CreateCore(instance, null, FallbackSignature, unrollFactor);

    public static BenchmarkAction CreateGlobalSetup(Descriptor descriptor, object instance) =>
        CreateCore(instance, descriptor.GlobalSetupMethod, FallbackSignature, 1);

    public static BenchmarkAction CreateGlobalCleanup(Descriptor descriptor, object instance) =>
        CreateCore(instance, descriptor.GlobalCleanupMethod, FallbackSignature, 1);

    public static BenchmarkAction CreateIterationSetup(Descriptor descriptor, object instance) =>
        CreateCore(instance, descriptor.IterationSetupMethod, FallbackSignature, 1);

    public static BenchmarkAction CreateIterationCleanup(Descriptor descriptor, object instance) =>
        CreateCore(instance, descriptor.IterationCleanupMethod, FallbackSignature, 1);
}