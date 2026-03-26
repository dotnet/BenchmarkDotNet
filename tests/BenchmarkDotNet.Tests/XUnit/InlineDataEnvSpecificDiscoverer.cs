using Xunit.Abstractions;
using Xunit.Sdk;

namespace BenchmarkDotNet.Tests.XUnit;

public class InlineDataEnvSpecificDiscoverer : IDataDiscoverer
{
    public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
    {
        // InlineDataEnvSpecific has two constructors:
        // 1. InlineDataEnvSpecific(object data, string reason, params EnvRequirement[] requirements)
        // 2. InlineDataEnvSpecific(object[] data, string reason, params EnvRequirement[] requirements)
        // GetConstructorArguments returns arguments from the constructor that was actually called

        var args = dataAttribute.GetConstructorArguments().ToArray();
        if (args.Length == 0)
            yield break;

        // First argument is either a single object or object[] - wrap accordingly
        if (args[0] is object[] dataArray)
        {
            // Array constructor was used
            yield return dataArray;
        }
        else
        {
            // Single object constructor was used - wrap in array
            yield return new[] { args[0] };
        }
    }

    public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod) => true;
}
