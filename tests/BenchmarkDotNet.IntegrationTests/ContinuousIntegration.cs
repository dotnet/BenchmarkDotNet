using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class ContinuousIntegration
    {
        internal static bool IsGitHubActionsOnWindows()
            => RuntimeInformation.IsWindows() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));

        internal static bool IsAppVeyorOnWindows()
            => RuntimeInformation.IsWindows() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR"));
    }
}
