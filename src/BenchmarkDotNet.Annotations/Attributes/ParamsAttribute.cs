using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : PriorityAttribute
    {
        public object[] Values { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        public ParamsAttribute() => Values = new object[0];

        public ParamsAttribute(params object[] values)
            => Values = values ?? new object[] { null }; // when users do Params(null) they mean one, null argument
    }
}
