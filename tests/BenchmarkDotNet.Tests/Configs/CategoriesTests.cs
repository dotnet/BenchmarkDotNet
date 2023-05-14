using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Configs
{
    public class CategoriesTests
    {
        private readonly ITestOutputHelper output;

        public CategoriesTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private void Check<T>(params string[] expected)
        {
            string Format(BenchmarkCase benchmarkCase) =>
                benchmarkCase.Descriptor.WorkloadMethod.Name + ": " +
                string.Join("+", benchmarkCase.Descriptor.Categories.OrderBy(category => category));

            var actual = BenchmarkConverter
                .TypeToBenchmarks(typeof(T))
                .BenchmarksCases
                .OrderBy(x => x.Descriptor.WorkloadMethod.Name)
                .Select(Format)
                .ToList();
            foreach (string s in actual)
                output.WriteLine(s);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CategoryInheritanceTest() =>
            Check<CategoryInheritanceTestScope.DerivedClass>(
                "BaseMethod: BaseClassCategory+BaseMethodCategory+DerivedClassCategory",
                "DerivedMethod: BaseClassCategory+DerivedClassCategory+DerivedMethodCategory"
            );

        public static class CategoryInheritanceTestScope
        {
            [BenchmarkCategory("BaseClassCategory")]
            public class BaseClass
            {
                [Benchmark]
                [BenchmarkCategory("BaseMethodCategory")]
                public void BaseMethod() { }
            }

            [BenchmarkCategory("DerivedClassCategory")]
            public class DerivedClass : BaseClass
            {
                [Benchmark]
                [BenchmarkCategory("DerivedMethodCategory")]
                public void DerivedMethod() { }
            }
        }

        [Fact]
        public void CategoryNoInheritanceTest() =>
            Check<CategoryNoInheritanceTestScope.DerivedClass>(
                "BaseMethod: BaseMethodCategory+DerivedClassCategory",
                "DerivedMethod: DerivedClassCategory+DerivedMethodCategory"
            );

        public static class CategoryNoInheritanceTestScope
        {
            [BenchmarkCategory("BaseClassCategory")]
            public class BaseClass
            {
                [Benchmark]
                [BenchmarkCategory("BaseMethodCategory")]
                public void BaseMethod() { }
            }

            [BenchmarkCategory("DerivedClassCategory")]
            [CategoryDiscoverer(false)]
            public class DerivedClass : BaseClass
            {
                [Benchmark]
                [BenchmarkCategory("DerivedMethodCategory")]
                public void DerivedMethod() { }
            }
        }

        [Fact]
        public void CustomCategoryDiscovererTest() =>
            Check<CustomCategoryDiscovererTestScope.Benchmarks>(
                "Aaa: A+PermanentCategory",
                "Bbb: B+PermanentCategory"
            );

        public static class CustomCategoryDiscovererTestScope
        {
            private class CustomCategoryDiscoverer : ICategoryDiscoverer
            {
                public string[] GetCategories(MethodInfo method)
                {
                    return new[]
                    {
                        "PermanentCategory",
                        method.Name.Substring(0, 1)
                    };
                }
            }

            [AttributeUsage(AttributeTargets.Class)]
            private class CustomCategoryDiscovererAttribute : Attribute, IConfigSource
            {
                public CustomCategoryDiscovererAttribute()
                {
                    Config = ManualConfig.CreateEmpty().WithCategoryDiscoverer(new CustomCategoryDiscoverer());
                }

                public IConfig Config { get; }
            }


            [CustomCategoryDiscoverer]
            public class Benchmarks
            {
                [Benchmark]
                public void Aaa() { }

                [Benchmark]
                public void Bbb() { }
            }
        }
    }
}