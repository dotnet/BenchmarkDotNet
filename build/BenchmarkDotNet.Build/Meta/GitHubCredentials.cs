using System;

namespace BenchmarkDotNet.Build.Meta;

public static class GitHubCredentials
{
    public const string TokenVariableName = "GITHUB_TOKEN";

    public const string ProductHeader = "BenchmarkDotNet";
    public static string? Token => Environment.GetEnvironmentVariable(TokenVariableName);
}