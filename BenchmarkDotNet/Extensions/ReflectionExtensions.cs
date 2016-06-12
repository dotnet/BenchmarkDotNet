using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        public static T ResolveAttribute<T>(this Type type) where T : Attribute =>
            type?.GetCustomAttributes<T>(typeof(T), false).FirstOrDefault();

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
            if (type == typeof(void))
                return "void";
            var prefix = "";
            if (!string.IsNullOrEmpty(type.Namespace))
                prefix += type.Namespace + ".";
            if (type.IsNested && type.DeclaringType != null)
                prefix += type.DeclaringType.Name + ".";

            if (type.IsGenericParameter())
                return type.Name;
            if (type.IsGenericType())
            {
                var mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
                string args = string.Join(", ", type.GetGenericArguments().Select(GetCorrectTypeName).ToArray());
                return $"{prefix}{mainName}<{args}>";
            }

            if (type.IsArray)
                return GetCorrectTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

            return prefix + type.Name;
        }

        internal static string GetTargetFrameworkVersion(this Assembly assembly)
        {
            var targetFrameworkAttribute = assembly.GetCustomAttributes<TargetFrameworkAttribute>(false).FirstOrDefault();
            if (targetFrameworkAttribute == null)
            {
                return "v4.0";
            }

            var frameworkName = new FrameworkName(targetFrameworkAttribute.FrameworkName);

            return $"v{frameworkName.Version}";
        }
    }
}