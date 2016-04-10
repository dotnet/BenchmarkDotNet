using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OrderProviderAttribute : Attribute, IConfigSource
    {
        public OrderProviderAttribute(
            SummaryOrderPolicy summaryOrderPolicy = SummaryOrderPolicy.Default, 
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared)
        {
            Config = ManualConfig.CreateEmpty().With(new DefaultOrderProvider(summaryOrderPolicy, methodOrderPolicy));
        }

        public IConfig Config { get; }
    }
}