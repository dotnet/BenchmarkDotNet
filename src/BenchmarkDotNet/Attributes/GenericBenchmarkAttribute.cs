using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GenericBenchmarkAttribute: Attribute
    {
        public Type[] GenericType { get; }

        public GenericBenchmarkAttribute(params Type[] genericType) => GenericType = genericType;
    }
}