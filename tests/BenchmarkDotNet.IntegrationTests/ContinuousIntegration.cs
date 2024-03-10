using BenchmarkDotNet.Portability;
using System;
using BenchmarkDotNet.Detectors;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class ContinuousIntegration
    {
        private static bool IsGitHubActions() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));

        internal static bool IsGitHubActionsOnWindows()
            => OsDetector.IsWindows() && IsGitHubActions();

        internal static bool IsLocalRun() => !IsGitHubActions();
    }
}
