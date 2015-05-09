using System;

namespace BenchmarkDotNet
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