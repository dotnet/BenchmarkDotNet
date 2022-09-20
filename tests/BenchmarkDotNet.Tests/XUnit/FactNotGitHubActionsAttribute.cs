using System;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class FactNotGitHubActionsAttribute : FactAttribute
    {
        private const string Message = "Test is not available on GitHub Actions";
        private static readonly string skip;

        static FactNotGitHubActionsAttribute()
        {
            string value = Environment.GetEnvironmentVariable("GITHUB_WORKFLOW"); // https://docs.github.com/en/actions/learn-github-actions/environment-variables
            skip = !string.IsNullOrEmpty(value) ? Message : null;
        }

        public override string Skip => skip;
    }
}