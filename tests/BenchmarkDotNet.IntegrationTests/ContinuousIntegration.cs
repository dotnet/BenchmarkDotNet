using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class ContinuousIntegration
    {
        private static bool IsGitHubActions() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));

        internal static bool IsGitHubActionsOnWindows()
            => RuntimeInformation.IsWindows() && IsGitHubActions();

        internal static bool IsLocalRun() => !IsGitHubActions();
    }
}
