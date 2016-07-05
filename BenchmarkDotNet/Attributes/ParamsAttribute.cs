using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ParamsAttribute : Attribute
    {
        public object[] Values { get; private set; }

        public ParamsAttribute(params object[] values)
        {
            Values = values;
        }
    }
}
