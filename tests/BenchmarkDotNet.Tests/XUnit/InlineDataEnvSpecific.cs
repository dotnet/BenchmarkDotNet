using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace BenchmarkDotNet.Tests.XUnit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InlineDataEnvSpecific : DataAttribute
{
    /// <summary>
    /// Gets the data to be passed to the test.
    /// </summary>
    // If the user passes null to the constructor, we assume what they meant was a
    // single null value to be passed to the test.
    public object?[] Data { get; }

    public InlineDataEnvSpecific(object? data, string reason, params EnvRequirement[] requirements)
        : this([data], reason, requirements) { }

    public InlineDataEnvSpecific(object?[] data, string reason, params EnvRequirement[] requirements)
    {
        // If the user passes null to the constructor, we assume what they meant was a
        // single null value to be passed to the test.
        Data = data ?? [null];
        string? skip = EnvRequirementChecker.GetSkip(requirements);
        if (skip != null)
            Skip = $"{skip} ({reason})";
    }

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod,
        DisposalTracker disposalTracker)
    {
        var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        TestIntrospectionHelper.MergeTraitsInto(traits, Traits);

        return new([
            new TheoryDataRow(Data)
            {
                Explicit = ExplicitAsNullable,
                Label = Label,
                Skip = Skip,
                TestDisplayName = TestDisplayName,
                Timeout = TimeoutAsNullable,
                Traits = traits,
            }
        ]);
    }

    public override bool SupportsDiscoveryEnumeration()
        => true;
}
