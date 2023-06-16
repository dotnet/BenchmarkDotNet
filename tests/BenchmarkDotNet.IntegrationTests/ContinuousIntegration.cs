using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class ContinuousIntegration
    {
        private static bool IsGitHubActions() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));

        private static bool IsAppVeyor() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR"));

        internal static bool IsGitHubActionsOnWindows()
            => RuntimeInformation.IsWindows() && IsGitHubActions();

        internal static bool IsAppVeyorOnWindows()
            => RuntimeInformation.IsWindows() && IsAppVeyor();

        internal static bool IsLocalRun() => !IsGitHubActions() && !IsAppVeyor();
    }
}
