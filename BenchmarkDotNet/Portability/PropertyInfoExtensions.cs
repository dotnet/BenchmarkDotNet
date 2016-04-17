using System.Reflection;

namespace BenchmarkDotNet.Portability
{
    internal static class PropertyInfoExtensions
    {
        internal static MethodInfo GetSetter(this PropertyInfo propertyInfo)
        {
#if !CORE
            return propertyInfo.GetSetMethod();
#else
            return propertyInfo.SetMethod;
#endif
        }
    }
}