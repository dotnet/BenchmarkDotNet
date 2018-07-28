using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ArgumentsAttribute : Attribute
    {
        public object[] Values { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        public ArgumentsAttribute()
        {
            Values = Array.Empty<object>();
        }

        public ArgumentsAttribute(params object[] values)
        {
            Values = values ?? new object[] { null }; // when users do Arguments(null) they mean one, null argument
        }
    }
}