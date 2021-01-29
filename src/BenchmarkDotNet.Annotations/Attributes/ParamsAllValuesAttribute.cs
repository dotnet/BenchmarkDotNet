using System;
namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAllValuesAttribute : PriorityAttribute
    {
    }
}