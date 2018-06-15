using System;
using JetBrains.Annotations;

namespace ChangeLogBuilder
{
    public class Config
    {
        [PublicAPI] public string ProductHeader => Environment.GetEnvironmentVariable("GITHUB_PRODUCT");
        [PublicAPI] public string Token => Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        [PublicAPI] public string RepoOwner => "dotnet";
        [PublicAPI] public string RepoName => "BenchmarkDotNet";
        [PublicAPI] public string CurrentMilestone { get; }

        [PublicAPI] public string PreviousMilestone { get; }
        [PublicAPI] public string LastCommit { get; }

        public void Deconstruct(out string repoOwner, out string repoName, out string currentMilestone,
            out string previousMilestone, out string lastCommit)
        {
            repoOwner = RepoOwner;
            repoName = RepoName;
            currentMilestone = CurrentMilestone;
            previousMilestone = PreviousMilestone;
            lastCommit = LastCommit;
        }

        public Config(string[] args)
        {
            CurrentMilestone = args[0];
            PreviousMilestone = args[1];
            LastCommit = args.Length <= 2 ? CurrentMilestone : args[2];
        }
    }
}