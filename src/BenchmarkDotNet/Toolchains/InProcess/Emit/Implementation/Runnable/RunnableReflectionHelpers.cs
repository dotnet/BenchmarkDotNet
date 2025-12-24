using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal static class RunnableReflectionHelpers
    {
        public const BindingFlags BindingFlagsNonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        public const BindingFlags BindingFlagsPublicInstance = BindingFlags.Public | BindingFlags.Instance;
        public const BindingFlags BindingFlagsPublicStatic = BindingFlags.Public | BindingFlags.Static;
        public const BindingFlags BindingFlagsAllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        public const BindingFlags BindingFlagsAllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static object TryChangeType(object value, Type targetType)
        {
            try
            {
                return targetType.IsInstanceOfType(value)
                    ? value
                    : Convert.ChangeType(value, targetType);
            }
            catch (InvalidCastException)
            {
            }

            if (value != null)
            {
                var implicitOp = GetImplicitConversionOpFromTo(value.GetType(), targetType);
                if (implicitOp != null)
                    return implicitOp.Invoke(null, [value]);
            }

            return value;
        }

        public static MethodInfo GetImplicitConversionOpFromTo(Type from, Type to)
        {
            return GetImplicitConversionOpCore(to, from, to)
                ?? GetImplicitConversionOpCore(from, from, to);
        }

        private static MethodInfo GetImplicitConversionOpCore(Type owner, Type from, Type to)
        {
            return owner.GetMethods(BindingFlagsPublicStatic)
                .FirstOrDefault(m =>
                    m.Name == OpImplicitMethodName
                    && m.ReturnType == to
                    && m.GetParameters().Single().ParameterType == from);
        }

        public static void SetArgumentField(object instance, BenchmarkCase benchmarkCase, ParameterInfo argInfo, int argIndex)
        {
            var argValue = benchmarkCase.Parameters.GetArgument(argInfo.Name)
                ?? throw new InvalidOperationException($"Can't find arg member for {argInfo.Name}.");

            var containerField = instance.GetType().GetField(FieldsContainerName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("FieldsContainer field not found on runnable instance.");

            var container = containerField.GetValue(instance);

            var argName = ArgFieldPrefix + argIndex;

            var argField = container.GetType().GetField(argName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException($"Can't find arg member {argName} inside FieldsContainer.");

            argField.SetValue(container, TryChangeType(argValue.Value, argField.FieldType));
            containerField.SetValue(instance, container);
        }

        public static void SetParameter(object instance, ParameterInstance paramInfo)
        {
            var instanceArg = paramInfo.IsStatic ? null : instance;
            var bindingFlags = paramInfo.IsStatic ? BindingFlagsAllStatic : BindingFlagsAllInstance;
            var type = instance.GetType();

            if (type.GetProperty(paramInfo.Name, bindingFlags) is var p && p != null)
            {
                p.SetValue(instanceArg, TryChangeType(paramInfo.Value, p.PropertyType));
            }
            else if (type.GetField(paramInfo.Name, bindingFlags) is var f && f != null)
            {
                f.SetValue(instanceArg, TryChangeType(paramInfo.Value, f.FieldType));
            }
            else
            {
                throw new InvalidOperationException($"Can't find a member {paramInfo.ToDisplayText()}.");
            }
        }

        public static Func<ValueTask> SetupOrCleanupCallbackFromMethod(object instance, string memberName)
        {
            return GetDelegateCore<Func<ValueTask>>(instance, memberName);
        }

        public static Func<long, IClock, ValueTask<ClockSpan>> LoopCallbackFromMethod(object instance, string memberName)
        {
            return GetDelegateCore<Func<long, IClock, ValueTask<ClockSpan>>>(instance, memberName);
        }

        private static TDelegate GetDelegateCore<TDelegate>(object instance, string memberName)
        {
            var result = instance.GetType().GetMethod(
                memberName,
                BindingFlagsAllInstance);
            if (result == null)
                throw new InvalidOperationException($"Can't find a member {memberName}.");

            return (TDelegate)(object)Delegate.CreateDelegate(typeof(TDelegate), instance, result);
        }
    }
}