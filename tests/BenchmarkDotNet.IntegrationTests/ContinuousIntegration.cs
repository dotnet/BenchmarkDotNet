using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tests.XUnit;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class ContinuousIntegration
    {
        private static bool IsGitHubActions() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));

        internal static bool IsGitHubActionsOnWindows()
            => OsDetector.IsWindows() && IsGitHubActions();

        internal static bool IsLocalRun() => !IsGitHubActions();

        internal static bool IsGitHubDraftPR()
           => EnvRequirementChecker.GetSkip(EnvRequirement.NonGitHubDraftPR).IsNotBlank();
    }
}
