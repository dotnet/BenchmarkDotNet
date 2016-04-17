using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Portability
{
    internal static class MemberInfoExtensions
    {
        internal static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo memberInfo, bool inherit)
            where T : Attribute
        {
#if !CORE
            return memberInfo.GetCustomAttributes(inherit).OfType<T>();
#else
            return CustomAttributeExtensions.GetCustomAttributes(memberInfo, typeof(T)).OfType<T>();
#endif
        }
    }
}