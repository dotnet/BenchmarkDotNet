using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CategoryDiscovererAttribute : Attribute, IConfigSource
    {
        public CategoryDiscovererAttribute(bool inherit = true)
        {
            Config = ManualConfig.CreateEmpty().WithCategoryDiscoverer(new DefaultCategoryDiscoverer(inherit));
        }

        public IConfig Config { get; }
    }
}