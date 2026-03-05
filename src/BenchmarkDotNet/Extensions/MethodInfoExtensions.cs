#if NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Extensions;

internal static class MethodInfoExtensions
{
    public static T CreateDelegate<T>(this MethodInfo methodInfo)
        where T : Delegate
    {
        return (T)methodInfo.CreateDelegate(typeof(T));
    }

    public static T CreateDelegate<T>(this MethodInfo methodInfo, object? target)
       where T : Delegate
    {
        return (T)methodInfo.CreateDelegate(typeof(T), target);
    }

}
#endif