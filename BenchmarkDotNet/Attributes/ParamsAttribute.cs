using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : Attribute
    {
        public object[] Values { get; private set; }

        public ParamsAttribute(params object[] values)
        {
            Values = values;
        }
    }
}
