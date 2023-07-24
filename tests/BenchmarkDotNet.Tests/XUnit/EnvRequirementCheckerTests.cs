using System;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit;

public class EnvRequirementCheckerTests
{
    [Fact]
    public void AllEnvRequirementsAreSupported()
    {
        foreach (var envRequirement in Enum.GetValues(typeof(EnvRequirement)).Cast<EnvRequirement>())
            EnvRequirementChecker.GetSkip(envRequirement);
    }
}