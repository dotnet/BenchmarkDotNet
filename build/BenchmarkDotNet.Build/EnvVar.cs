using System;

namespace BenchmarkDotNet.Build;

public class EnvVar
{
    public static readonly EnvVar GitHubToken = new("GITHUB_TOKEN");
    public static readonly EnvVar NuGetToken = new("NUGET_TOKEN");

    public string Name { get; }

    private EnvVar(string name) => Name = name;

    public string? GetValue() => Environment.GetEnvironmentVariable(Name);

    public void AssertHasValue()
    {
        if (string.IsNullOrEmpty(GetValue()))
            throw new Exception($"Environment variable '{Name}' is not specified!");
    }

    public void SetEmpty() => Environment.SetEnvironmentVariable(Name, "");
}