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

        /// <summary>
        /// returns type name which can be used in generated C# code without &amp; in the type name for by-ref
        /// </summary>
        internal static string GetCorrectCSharpTypeNameWithoutRef(this Type type)
            => GetCorrectCSharpTypeName(type).Replace("&", string.Empty);

        /// <summary>
        /// returns type name which can be used in generated C# code
        /// </summary>
        internal static string GetCorrectCSharpTypeName(this Type type)
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
                string args = string.Join(", ", type.GetGenericArguments().Select(GetCorrectCSharpTypeName).ToArray());
                return $"{prefix}{mainName}<{args}>";
            }

            if (type.IsArray)
                return GetCorrectCSharpTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

            return prefix + type.Name;
        }

        /// <summary>
        /// returns simple, human friendly display name
        /// </summary>
        internal static string GetDisplayName(this Type type) => GetDisplayName(type.GetTypeInfo());

        /// <summary>
        /// returns simple, human friendly display name
        /// </summary>
        internal static string GetDisplayName(this TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericType)
                return typeInfo.Name;
            if (typeInfo.IsGenericTypeDefinition)
                throw new NotSupportedException("Open generics are not supported");

            var mainName = typeInfo.Name.Substring(0, typeInfo.Name.IndexOf('`'));
            string args = string.Join(", ", typeInfo.GetGenericArguments().Select(GetDisplayName).ToArray());
            return $"{mainName}<{args}>";
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

            if (typeInfo.IsAbstract || typeInfo.IsSealed || typeInfo.IsNotPublic || (typeInfo.IsGenericType && !IsRunnableGenericType(typeInfo)))
                return false;

            return typeInfo.GetBenchmarks().Any();
        }

        internal static MethodInfo[] GetBenchmarks(this TypeInfo typeInfo)
            => typeInfo
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any())
                .ToArray();

        internal static (string Name, TAttribute Attribute, bool IsPrivate, bool IsStatic, Type ParameterType)[] GetTypeMembersWithGivenAttribute<TAttribute>(this Type type, BindingFlags reflectionFlags)
            where TAttribute : Attribute
        {
            var allFields = type.GetFields(reflectionFlags)
                                .Select(f => (
                                    Name: f.Name, 
                                    Attribute: f.ResolveAttribute<TAttribute>(),
                                    IsPrivate: f.IsPrivate,
                                    IsStatic: f.IsStatic, 
                                    ParameterType: f.FieldType));

            var allProperties = type.GetProperties(reflectionFlags)
                                    .Select(p => (
                                        Name: p.Name, 
                                        Attribute: p.ResolveAttribute<TAttribute>(), 
                                        IsPrivate: p.GetSetMethod() == null, 
                                        IsStatic: p.GetSetMethod() != null && p.GetSetMethod().IsStatic, 
                                        PropertyType: p.PropertyType));

            var joined = allFields.Concat(allProperties).Where(member => member.Attribute != null).ToArray();

            foreach (var member in joined.Where(m => m.IsPrivate))
                throw new InvalidOperationException($"Member \"{member.Name}\" must be public if it has the [{typeof(TAttribute).Name}] attribute applied to it");

            return joined;
        }

        private static bool IsRunnableGenericType(TypeInfo typeInfo)
            => !typeInfo.IsGenericTypeDefinition // is not an open generic
            && typeInfo.DeclaredConstructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0); // we need public parameterless ctor to create it
    }
}