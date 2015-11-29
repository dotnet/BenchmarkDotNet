using System;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        public static T ResolveAttribute<T>(this MethodInfo methodInfo) where T : Attribute
        {
            return methodInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        public static T ResolveAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute
        {
            return propertyInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        public static T ResolveAttribute<T>(this FieldInfo fieldInfo) where T : Attribute
        {
            return fieldInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        public static string GetCorrectTypeName(this Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            var mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
            string args = string.Join(", ", type.GetGenericArguments().Select(GetCorrectTypeName).ToArray());
            return $"{type.Namespace}.{mainName}<{args}>";
        }
    }
}