using System;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        public static T ResolveAttribute<T>(this Type type) where T : Attribute =>
            type?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        public static T ResolveAttribute<T>(this MethodInfo methodInfo) where T : Attribute =>
            methodInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        public static T ResolveAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute =>
            propertyInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        public static T ResolveAttribute<T>(this FieldInfo fieldInfo) where T : Attribute =>
            fieldInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        public static bool HasAttribute<T>(this MethodInfo methodInfo) where T : Attribute =>
            methodInfo.ResolveAttribute<T>() != null;

        public static string GetCorrectTypeName(this Type type)
        {
            if (!type.IsGenericType)
            {
                if (type.IsNested)
                    return $"{type.DeclaringType.Name}.{type.Name}";
                return type.Name;
            }

            var mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
            string args = string.Join(", ", type.GetGenericArguments().Select(GetCorrectTypeName).ToArray());
            if (type.IsNested)
            {
                return $"{type.Namespace}.{type.DeclaringType.Name}.{mainName}<{args}>";
            }
            return $"{type.Namespace}.{mainName}<{args}>";
        }
    }
}