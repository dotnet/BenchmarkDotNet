using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    // so here is the thing: .NET Standard 1.3 does not expose few things that are available for the frameworks that we target
    // so we invoke some stuff via reflection to workaround it
    // these methods are not called frequently, so perf is not a problem
    public static class UglyDoNetStandardWorkarounds
    {
        public static TProperty ReadProperty<TType, TProperty>(this TType instance, string propertyName)
        {
            var property = typeof(TType).GetTypeInfo().GetDeclaredProperty(propertyName);

            return (TProperty)property.GetMethod.Invoke(instance, null);
        }

        public static TResult ExecuteMethod<TType, TResult>(this TType instance, string methodName)
        {
            var method = typeof(TType).GetTypeInfo().GetDeclaredMethod(methodName);

            return (TResult)method.Invoke(instance, null);
        }
    }
}