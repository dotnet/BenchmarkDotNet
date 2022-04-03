using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class GitHubActions
    {
        // temporary workaround for https://github.com/dotnet/BenchmarkDotNet/issues/1943
        internal static bool IsRunningOnWindows()
            => RuntimeInformation.IsWindows() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));
    }
}
