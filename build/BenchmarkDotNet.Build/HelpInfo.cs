using System;
using BenchmarkDotNet.Build.Options;

namespace BenchmarkDotNet.Build;

public class HelpInfo
{
    public string Description { get; init; } = "";
    public IOption[] Options { get; init; } = Array.Empty<IOption>();
    public string[] EnvironmentVariables { get; init; } = Array.Empty<string>();
    public Example[] Examples { get; init; } = Array.Empty<Example>();
}