using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace BenchmarkDotNet.Portability
{
    internal static class AssemblyExtensions
    {
        internal static IEnumerable<T> GetCustomAttributes<T>(this Assembly assembly, bool inherit)
            where T : Attribute
        {
#if !CORE
            return assembly.GetCustomAttributes(inherit).OfType<T>();
#else
            return CustomAttributeExtensions.GetCustomAttributes(assembly, typeof(T)).OfType<T>();
#endif
        }
    }
}