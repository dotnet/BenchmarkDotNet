using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Reflection;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark whose category comes from a custom <see cref="ICategoryDiscoverer"/> rather than from a
    /// [BenchmarkCategory]. The adapter has to publish the categories the config resolved: rediscovering them with the
    /// default discoverer would leave a <c>--treenode-filter</c> on a custom category matching nothing, even though
    /// BenchmarkDotNet's own --anyCategories and the summary do see it.
    /// </summary>
    [Config(typeof(DiscoveredCategoryConfig))]
    public class CategoryProbe
    {
        [Benchmark]
        public int Identity() => 1;

        private class CategoryFromMethodName : ICategoryDiscoverer
        {
            // The default discoverer only reads [BenchmarkCategory], so this category exists nowhere else.
            public string[] GetCategories(MethodInfo method) => [$"Discovered{method.Name}"];
        }

        private class DiscoveredCategoryConfig : ManualConfig
        {
            public DiscoveredCategoryConfig()
            {
                AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
                WithCategoryDiscoverer(new CategoryFromMethodName());
            }
        }
    }
}
