using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace BenchmarkDotNet.Tests.XUnit;

[DataDiscoverer("BenchmarkDotNet.Tests.XUnit.InlineDataEnvSpecificDiscoverer", "BenchmarkDotNet.Tests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InlineDataEnvSpecific : DataAttribute
{
    readonly object[] data;

    public InlineDataEnvSpecific(object data, string reason, params EnvRequirement[] requirements)
        : this([data], reason, requirements) { }

    public InlineDataEnvSpecific(object[] data, string reason, params EnvRequirement[] requirements)
    {
        this.data = data;
        string? skip = EnvRequirementChecker.GetSkip(requirements);
        if (skip != null)
            Skip = $"{skip} ({reason})";
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod) => [data];
}
