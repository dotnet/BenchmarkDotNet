using System.Runtime.CompilerServices;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit;

public class TheoryEnvSpecificAttribute : TheoryAttribute
{
    public TheoryEnvSpecificAttribute(params EnvRequirement[] requirements)
    {
        Skip = EnvRequirementChecker.GetSkip(requirements);
    }

    public TheoryEnvSpecificAttribute(string reason, params EnvRequirement[] requirements)
    {
        string? skip = EnvRequirementChecker.GetSkip(requirements);
        if (skip != null)
            Skip = $"{skip} ({reason})";
    }

    public TheoryEnvSpecificAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }
}