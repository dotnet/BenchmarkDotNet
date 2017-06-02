using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        internal static T ResolveAttribute<T>(this Type type) where T : Attribute =>
            type?.GetTypeInfo().GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();

        internal static T ResolveAttribute<T>(this MethodInfo methodInfo) where T : Attribute =>
            methodInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static T ResolveAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute =>
            propertyInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static T ResolveAttribute<T>(this FieldInfo fieldInfo) where T : Attribute =>
            fieldInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static bool HasAttribute<T>(this MethodInfo methodInfo) where T : Attribute =>
            methodInfo.ResolveAttribute<T>() != null;

        internal static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

        internal static string GetCorrectTypeName(this Type type)
        {
            if (type == typeof(void))
                return "void";
            var prefix = "";
            if (!string.IsNullOrEmpty(type.Namespace))
                prefix += type.Namespace + ".";

            var nestedTypes = "";
            Type child = type, parent = type.DeclaringType;
            while (child.IsNested && parent != null)
            {
                nestedTypes = parent.Name + "." + nestedTypes;

                child = parent;
                parent = parent.DeclaringType;
            }
            prefix += nestedTypes;
                

            if (type.GetTypeInfo().IsGenericParameter)
                return type.Name;
            if (type.GetTypeInfo().IsGenericType)
            {
                var mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
                string args = string.Join(", ", type.GetGenericArguments().Select(GetCorrectTypeName).ToArray());
                return $"{prefix}{mainName}<{args}>";
            }

            if (type.IsArray)
                return GetCorrectTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

            return prefix + type.Name;
        }

        internal static IEnumerable<MethodInfo> GetAllMethods(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var methodInfo in typeInfo.DeclaredMethods)
                    yield return methodInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var fieldInfo in typeInfo.DeclaredFields)
                    yield return fieldInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var propertyInfo in typeInfo.DeclaredProperties)
                    yield return propertyInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static bool IsStruct(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsValueType && !typeInfo.IsPrimitive;
        }

        internal static Type[] GetRunnableBenchmarks(this Assembly assembly)
            => assembly
                .GetTypes()
                .Where(type => type.ContainsRunnableBenchmarks())
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();

        internal static bool ContainsRunnableBenchmarks(this Type type)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsAbstract || typeInfo.IsSealed || typeInfo.IsGenericType || typeInfo.IsNotPublic)
                return false;

            return typeInfo.GetBenchmarks().Any();
        }

        internal static MethodInfo[] GetBenchmarks(this TypeInfo typeInfo)
            => typeInfo
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any())
                .ToArray();
    }
}