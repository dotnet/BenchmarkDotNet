using System;

namespace BenchmarkDotNet.Attributes
{
    public class OperationsPerInvokeAttribute : Attribute
    {
        public long Count { get; }

        public OperationsPerInvokeAttribute(long count)
        {
            Count = count;
        }
    }
}