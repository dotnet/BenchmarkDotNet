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
            MethodOrderPolicy methodOrderPolicy = MethodOrderPolicy.Declared,
            JobOrderPolicy jobOrderPolicy = JobOrderPolicy.Default)
        {
            Config = ManualConfig.CreateEmpty().WithOrderer(new DefaultOrderer(summaryOrderPolicy, methodOrderPolicy, jobOrderPolicy));
        }

        public IConfig Config { get; }
    }
}