using System;

namespace BenchmarkDotNet.Attributes
{
    public class OperationCountAttribute : Attribute
    {
        public long Count { get; }

        public OperationCountAttribute(long count)
        {
            Count = count;
        }
    }
}