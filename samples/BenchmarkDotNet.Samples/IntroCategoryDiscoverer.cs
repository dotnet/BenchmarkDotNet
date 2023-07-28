using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    [CategoriesColumn]
    [CustomCategoryDiscoverer]
    public class IntroCategoryDiscoverer
    {
        private class CustomCategoryDiscoverer : DefaultCategoryDiscoverer
        {
            public override string[] GetCategories(MethodInfo method)
            {
                var categories = new List<string>();
                categories.AddRange(base.GetCategories(method));
                categories.Add("All");
                categories.Add(method.Name.Substring(0, 1));
                return categories.ToArray();
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        private class CustomCategoryDiscovererAttribute : Attribute, IConfigSource
        {
            public CustomCategoryDiscovererAttribute()
            {
                Config = ManualConfig.CreateEmpty()
                    .WithCategoryDiscoverer(new CustomCategoryDiscoverer());
            }

            public IConfig Config { get; }
        }


        [Benchmark]
        public void Foo() { }

        [Benchmark]
        public void Bar() { }
    }
}