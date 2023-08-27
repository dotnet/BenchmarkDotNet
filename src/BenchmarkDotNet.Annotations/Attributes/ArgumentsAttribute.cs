using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ArgumentsAttribute : PriorityAttribute
    {
        public object?[] Values { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        public ArgumentsAttribute() => Values = new object[0];

        public ArgumentsAttribute(params object?[]? values)
            => Values = values ?? new object?[] { null }; // when users do Arguments(null) they mean one, null argument
    }
}