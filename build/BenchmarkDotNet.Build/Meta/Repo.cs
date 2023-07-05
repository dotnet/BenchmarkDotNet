using System;

namespace BenchmarkDotNet.Build.Meta;

public static class Repo
{
    public const string Owner = "dotnet";
    public const string Name = "BenchmarkDotNet";
    public const string HttpsUrlBase = $"https://github.com/{Owner}/{Name}";
    public const string HttpsGitUrl =  $"{HttpsUrlBase}.git";
    public const string ChangelogDetailsBranch = "docs-changelog-details";

    public const string ProductHeaderVar = "GITHUB_PRODUCT";
    public const string TokenVar = "GITHUB_TOKEN";

    public static string? ProductHeader => Environment.GetEnvironmentVariable(ProductHeaderVar);
    public static string? Token => Environment.GetEnvironmentVariable(TokenVar);
}