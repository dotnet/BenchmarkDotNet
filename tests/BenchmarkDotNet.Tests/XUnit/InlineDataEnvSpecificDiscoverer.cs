using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace BenchmarkDotNet.Tests.XUnit;

public class InlineDataEnvSpecificDiscoverer : IDataDiscoverer
{
    public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
    {
        // InlineDataEnvSpecific stores the test data in its first constructor parameter (object[] data)
        // The other parameters (reason, requirements) are used for determining Skip status
        // We just need to extract and return the data array
        var args = dataAttribute.GetConstructorArguments().ToArray();

        // First argument should be the object[] data
        if (args.Length > 0 && args[0] is object[] data)
        {
            yield return data;
        }
    }

    public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod) => true;
}
