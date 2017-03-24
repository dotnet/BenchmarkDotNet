using System;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ParameterComparerTests
    {
        [Fact]
        public void BasicComparisionTest()
        {
            var comparer = ParameterComparer.Instance;

            var sharedDefinition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>());
            var originalData = new[]
            {
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 5),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 1),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 3),
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

            var sharedDefinition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>());
            var originalData = new []
            {
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 5),
                    new ParameterInstance(sharedDefinition, "z"),
                    new ParameterInstance(sharedDefinition, 1.0),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 5),
                    new ParameterInstance(sharedDefinition, "a"),
                    new ParameterInstance(sharedDefinition, 0.0),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 5),
                    new ParameterInstance(sharedDefinition, "a"),
                    new ParameterInstance(sharedDefinition, 1.0),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 3),
                    new ParameterInstance(sharedDefinition, "a"),
                    new ParameterInstance(sharedDefinition, 97.5)
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

            var sharedDefinition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>());
            var originalData = new[]
            {
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 100),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 1000),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 2000),
                }),
                new ParameterInstances(new []
                {
                    new ParameterInstance(sharedDefinition, 500),
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
