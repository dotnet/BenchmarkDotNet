using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace BenchmarkDotNet.Tests.Attributes
{
    public class MutatorAttributesTests
    {
        [Fact]
        public void AllMutatorsAttributesCanBeAppliedOnlyOncePerType()
        {
            var attributeTypes = typeof(JobMutatorConfigBaseAttribute)
                .Assembly
                .GetExportedTypes()
                .Where(type => type.IsSubclassOf(typeof(JobMutatorConfigBaseAttribute)));

            foreach (var attributeType in attributeTypes)
            {
                Assert.Single(
                    attributeType.GetCustomAttributes().OfType<AttributeUsageAttribute>(),
                    usageAttribute => usageAttribute.AllowMultiple == false);
            }
        }
    }
}