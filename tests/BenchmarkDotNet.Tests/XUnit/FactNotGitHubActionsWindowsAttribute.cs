using System;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class FactNotGitHubActionsWindowsAttribute : FactAttribute
    {
        private const string Message = "Test is not available on GitHub Actions Windows";
        private static readonly string skip;

        static FactNotGitHubActionsWindowsAttribute()
        {
            string value = Environment.GetEnvironmentVariable("GITHUB_WORKFLOW"); // https://docs.github.com/en/actions/learn-github-actions/environment-variables
            skip = !string.IsNullOrEmpty(value) && RuntimeInformation.IsWindows() ? Message : null;
        }

        public override string Skip => skip;
    }
}