using System.Runtime.CompilerServices;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit;

public class FactEnvSpecificAttribute : FactAttribute
{
    public FactEnvSpecificAttribute(params EnvRequirement[] requirements)
    {
        Skip = EnvRequirementChecker.GetSkip(requirements);
    }

    public FactEnvSpecificAttribute(string reason, params EnvRequirement[] requirements)
    {
        string? skip = EnvRequirementChecker.GetSkip(requirements);
        if (skip != null)
            Skip = $"{skip} ({reason})";
    }

    public FactEnvSpecificAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }
}