using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Portability
{
    internal static class TypeExtensions
    {
        internal static System.Reflection.Assembly Assembly(this Type type)
        {
#if !CORE
            return type.Assembly;
#else
            return type.GetTypeInfo().Assembly;
#endif
        }

        internal static bool IsPublic(this Type type)
        {
#if !CORE
            return type.IsPublic;
#else
            return type.GetTypeInfo().IsPublic;
#endif
        }

        internal static bool IsGenericType(this Type type)
        {
#if !CORE
            return type.IsGenericType;
#else
            return type.GetTypeInfo().IsGenericType;
#endif
        }

        internal static bool IsGenericParameter(this Type type)
        {
#if !CORE
            return type.IsGenericParameter;
#else
            return type.GetTypeInfo().IsGenericParameter;
#endif
        }

        internal static bool IsNested(this Type type)
        {
#if !CORE
            return type.IsNested;
#else
            return type.GetTypeInfo().IsNested;
#endif
        }

        internal static bool IsNestedPublic(this Type type)
        {
#if !CORE
            return type.IsNestedPublic;
#else
            return type.GetTypeInfo().IsNestedPublic;
#endif
        }

        internal static bool IsValueType(this Type type)
        {
#if !CORE
            return type.IsValueType;
#else
            return type.GetTypeInfo().IsValueType;
#endif
        }

        internal static bool IsEnum(this Type type)
        {
#if !CORE
            return type.IsEnum;
#else
            return type.GetTypeInfo().IsEnum;
#endif
        }

        internal static IEnumerable<T> GetCustomAttributes<T>(this Type sourceType, bool inherit)
        {
#if !CORE
            return sourceType.GetCustomAttributes(inherit).OfType<T>();
#else
            return sourceType.GetTypeInfo().GetCustomAttributes(inherit).OfType<T>();
#endif
        }

        internal static IEnumerable<T> GetCustomAttributes<T>(this Type sourceType, Type attributeType, bool inherit)
        {
#if !CORE
            return sourceType.GetCustomAttributes(attributeType, inherit).OfType<T>();
#else
            return sourceType.GetTypeInfo().GetCustomAttributes(attributeType, inherit).OfType<T>();
#endif
        }

        internal static Type BaseType(this Type type)
        {
#if !CORE
            return type.BaseType;
#else
            return type.GetTypeInfo().BaseType;
#endif
        }
    }
}