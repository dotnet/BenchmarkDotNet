using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
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
                    return implicitOp.Invoke(null, new[] { value });
            }

            return value;
        }

        public static bool IsRefLikeType(Type t)
        {
            return t.IsValueType
                && t.GetCustomAttributes().Any(a => a.GetType().FullName == IsByRefLikeAttributeTypeName);
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

        public static void SetArgumentField<T>(
            T instance,
            BenchmarkCase benchmarkCase,
            ParameterInfo argInfo,
            int argIndex)
        {
            var argValue = benchmarkCase.Parameters.GetArgument(argInfo.Name);
            if (argValue == null)
            {
                throw new InvalidOperationException($"Can't find arg member for {argInfo.Name}.");
            }

            var type = instance.GetType();
            var argName = ArgFieldPrefix + argIndex;
            if (type.GetField(argName, BindingFlagsNonPublicInstance) is var f && f != null)
            {
                f.SetValue(instance, TryChangeType(argValue.Value, f.FieldType));
            }
            else
            {
                throw new InvalidOperationException($"Can't find arg member for {argInfo.Name}.");
            }
        }

        public static void SetParameter<T>(
            T instance,
            ParameterInstance paramInfo)
        {
            var instanceArg = paramInfo.IsStatic ? null : (object)instance;
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

        public static Action CallbackFromField<T>(T instance, string memberName)
        {
            return GetFieldValueCore<T, Action>(instance, memberName);
        }

        public static Action CallbackFromMethod<T>(T instance, string memberName)
        {
            return GetDelegateCore<T, Action>(instance, memberName);
        }

        public static Action<long> LoopCallbackFromMethod<T>(T instance, string memberName)
        {
            return GetDelegateCore<T, Action<long>>(instance, memberName);
        }

        private static TResult GetFieldValueCore<T, TResult>(T instance, string memberName)
        {
            var result = instance.GetType().GetField(
                memberName,
                BindingFlagsAllInstance);
            if (result == null)
                throw new InvalidOperationException($"Can't find a member {memberName}.");

            return (TResult)result.GetValue(instance);
        }

        private static TDelegate GetDelegateCore<T, TDelegate>(T instance, string memberName)
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