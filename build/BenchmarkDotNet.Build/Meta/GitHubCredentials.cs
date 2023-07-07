using System;
using Octokit;

namespace BenchmarkDotNet.Build.Meta;

public static class GitHubCredentials
{
    public const string TokenVariableName = "GITHUB_TOKEN";

    public const string ProductHeader = "BenchmarkDotNet";
    public static string? Token => Environment.GetEnvironmentVariable(TokenVariableName);

    public static GitHubClient CreateClient()
    {
        var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
        var tokenAuth = new Credentials(Token);
        client.Credentials = tokenAuth;
        return client;
    }
}