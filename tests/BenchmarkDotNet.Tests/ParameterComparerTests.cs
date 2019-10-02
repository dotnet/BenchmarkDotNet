using System;
using BenchmarkDotNet.Parameters;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Tests
{
    public class ParameterComparerTests
    {
        [Fact]
        public void BasicComparisionTest()
        {
            var comparer = ParameterComparer.Instance;
            var config = DefaultConfig.Instance.CreateImmutableConfig();

            var sharedDefinition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>(), isArgument: false, parameterType: null);
            var originalData = new[]
            {
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 5, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 1, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 3, config)
                })
            };
            var sortedData = originalData.OrderBy(d => d, comparer).ToArray();

            Assert.Equal(1, sortedData[0].Items[0].Value);
            Assert.Equal(3, sortedData[1].Items[0].Value);
            Assert.Equal(5, sortedData[2].Items[0].Value);
        }

        [Fact]
        public void MultiParameterComparisionTest()
        {
            var comparer = ParameterComparer.Instance;
            var config = DefaultConfig.Instance.CreateImmutableConfig();

            var sharedDefinition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>(), isArgument: false, parameterType: null);
            var originalData = new[]
            {
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 5, config),
                    new ParameterInstance(sharedDefinition, "z", config),
                    new ParameterInstance(sharedDefinition, 1.0, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 5, config),
                    new ParameterInstance(sharedDefinition, "a", config),
                    new ParameterInstance(sharedDefinition, 0.0, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 5, config),
                    new ParameterInstance(sharedDefinition, "a", config),
                    new ParameterInstance(sharedDefinition, 1.0, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 3, config),
                    new ParameterInstance(sharedDefinition, "a", config),
                    new ParameterInstance(sharedDefinition, 97.5, config)
                })
            };

            // We should sort by the parameters in order, i.e. first by 5/3, then if they match by "z"/"a"
            var sortedData = originalData.OrderBy(d => d, comparer).ToArray();

            Assert.Equal(3, sortedData[0].Items[0].Value);
            Assert.Equal("a", sortedData[0].Items[1].Value);
            Assert.Equal(97.5, sortedData[0].Items[2].Value);

            Assert.Equal(5, sortedData[1].Items[0].Value);
            Assert.Equal("a", sortedData[1].Items[1].Value);
            Assert.Equal(0.0, sortedData[1].Items[2].Value);

            Assert.Equal(5, sortedData[2].Items[0].Value);
            Assert.Equal("a", sortedData[2].Items[1].Value);
            Assert.Equal(1.0, sortedData[2].Items[2].Value);

            Assert.Equal(5, sortedData[3].Items[0].Value);
            Assert.Equal("z", sortedData[3].Items[1].Value);
            Assert.Equal(1.0, sortedData[3].Items[2].Value);
        }

        [Fact]
        public void AlphaNumericComparisionTest()
        {
            var comparer = ParameterComparer.Instance;
            var config = DefaultConfig.Instance.CreateImmutableConfig();

            var sharedDefinition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>(), isArgument: false, parameterType: null);
            var originalData = new[]
            {
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 100, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 1000, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 2000, config)
                }),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(sharedDefinition, 500, config)
                })
            };

            var sortedData = originalData.OrderBy(d => d, comparer).ToArray();

            // Check that we sort by numeric value, not string order!!
            Assert.Equal(100, sortedData[0].Items[0].Value);
            Assert.Equal(500, sortedData[1].Items[0].Value);
            Assert.Equal(1000, sortedData[2].Items[0].Value);
            Assert.Equal(2000, sortedData[3].Items[0].Value);
        }
    }
}