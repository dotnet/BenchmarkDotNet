using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : Attribute
    {
        public object[] Values { get; private set; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        public ParamsAttribute()
        {
            Values = Array.Empty<object>();
        }

        public ParamsAttribute(params object[] values)
        {
            Values = values ?? new object[] { null }; // when users do Params(null) they mean one, null argument
        }
    }
}
