using System;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        public static T ResolveAttribute<T>(this MethodInfo methodInfo) where T : Attribute
        {
            return methodInfo.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }
    }
}