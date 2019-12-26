using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class OrdererAttribute : Attribute, IConfigSource
    {
        public OrdererAttribute(
            SummaryOrderPolicy summaryOrderPolicy = SummaryOrderPolicy.Default,
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared)
        {
            Config = ManualConfig.CreateEmpty().WithOrderer(new DefaultOrderer(summaryOrderPolicy, methodOrderPolicy));
        }

        public IConfig Config { get; }
    }
}