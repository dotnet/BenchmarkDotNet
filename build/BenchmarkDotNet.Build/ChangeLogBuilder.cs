using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Build.Helpers;
using BenchmarkDotNet.Build.Meta;
using Cake.Common.Diagnostics;
using Cake.Core.IO;
using Octokit;

namespace BenchmarkDotNet.Build;

public static class ChangeLogBuilder
{
    private class Config
    {
        public string CurrentVersion { get; }
        public string PreviousVersion { get; }
        public string LastCommit { get; }

        public void Deconstruct(out string currentMilestone, out string previousMilestone, out string lastCommit)
        {
            currentMilestone = CurrentVersion;
            previousMilestone = PreviousVersion;
            lastCommit = LastCommit;
        }

        public Config(string currentVersion, string previousVersion, string lastCommit)
        {
            CurrentVersion = currentVersion;
            PreviousVersion = previousVersion;
            LastCommit = lastCommit;
        }
    }

    private class MarkdownBuilder
    {
        private static IReadOnlyList<Milestone>? allMilestones;
        private static readonly Dictionary<string, string> AuthorNames = new();

        private readonly Config config;
        private readonly StringBuilder builder;

        public static async Task<string> Build(Config config)
        {
            return await new MarkdownBuilder(config).Build();
        }

        private MarkdownBuilder(Config config)
        {
            this.config = config;
            builder = new StringBuilder();
        }

        private async Task<string> Build()
        {
            var (currentVersion, previousVersion, lastCommit) = config;
            if (string.IsNullOrEmpty(lastCommit))
                lastCommit = $"v{currentVersion}";

            var client = Utils.CreateGitHubClient();

            if (currentVersion == "_")
            {
                var allContributors = await client.Repository.GetAllContributors(Repo.Owner, Repo.Name);
                builder.AppendLine("# All contributors");
                builder.AppendLine();
                foreach (var contributor in allContributors)
                {
                    var user = await client.User.Get(contributor.Login);
                    var name = user?.Name;
                    builder.AppendLine("* " + (string.IsNullOrEmpty(name)
                        ? contributor.ToLink()
                        : contributor.ToLinkWithName(name)));
                }

                return builder.ToString();
            }

            if (allMilestones == null)
            {
                var milestoneRequest = new MilestoneRequest
                {
                    State = ItemStateFilter.All
                };
                allMilestones =
                    await client.Issue.Milestone.GetAllForRepository(Repo.Owner, Repo.Name, milestoneRequest);
            }

            IReadOnlyList<Issue> allIssues = Array.Empty<Issue>();
            var targetMilestone = allMilestones.FirstOrDefault(m => m.Title == $"v{currentVersion}");
            if (targetMilestone != null)
            {
                var issueRequest = new RepositoryIssueRequest
                {
                    State = ItemStateFilter.Closed,
                    Milestone = targetMilestone.Number.ToString()
                };

                allIssues = await client.Issue.GetAllForRepository(Repo.Owner, Repo.Name, issueRequest);
            }

            var issues = allIssues
                .Where(issue => issue.PullRequest == null)
                .OrderBy(issue => issue.Number)
                .ToList();
            var pullRequests = allIssues
                .Where(issue => issue.PullRequest != null)
                .OrderBy(issue => issue.Number)
                .ToList();

            var compare =
                await client.Repository.Commit.Compare(Repo.Owner, Repo.Name, $"v{previousVersion}", lastCommit);
            var commits = compare.Commits;

            foreach (var contributor in commits.Select(commit => commit.Author))
                if (contributor != null && !AuthorNames.ContainsKey(contributor.Login))
                {
                    var user = await client.User.Get(contributor.Login);
                    var name = user?.Name;
                    AuthorNames[contributor.Login] = string.IsNullOrWhiteSpace(name) ? contributor.Login : name;
                }

            string PresentContributor(GitHubCommit commit)
            {
                if (commit.Author != null)
                    return $"{AuthorNames[commit.Author.Login]} ({commit.Author.ToLink()})".Trim();
                return commit.Commit.Author.Name;
            }

            var contributors = compare.Commits
                .Select(PresentContributor)
                .OrderBy(it => it)
                .Distinct()
                .ToImmutableList();

            var milestoneHtmlUlr = $"https://github.com/{Repo.Owner}/{Repo.Name}/issues?q=milestone:v{currentVersion}";

            builder.AppendLine("## Milestone details");
            builder.AppendLine();
            builder.AppendLine($"In the [v{currentVersion}]({milestoneHtmlUlr}) scope, ");
            builder.Append(issues.Count + " issues were resolved and ");
            builder.AppendLine(pullRequests.Count + " pull requests were merged.");
            builder.AppendLine($"This release includes {commits.Count} commits by {contributors.Count} contributors.");
            builder.AppendLine();

            AppendList("Resolved issues", issues, issue =>
                $"[#{issue.Number}]({issue.HtmlUrl}) {issue.Title.Trim()}{issue.Assignee.ToStr("assignee:")}");
            AppendList("Merged pull requests", pullRequests, pr =>
                $"[#{pr.Number}]({pr.HtmlUrl}) {pr.Title.Trim()}{pr.User.ToStr("by")}");
            AppendList("Commits", commits, commit =>
                $"{commit.ToLink()} {commit.Commit.ToCommitMessage()}{commit.ToByStr()}");
            AppendList("Contributors", contributors, it => it, "Thank you very much!");

            return builder.ToString();
        }

        private void AppendList<T>(string title, IReadOnlyList<T> items, Func<T, string> format,
            string? conclusion = null)
        {
            builder.AppendLine($"## {title} ({items.Count})");
            builder.AppendLine();
            foreach (var item in items)
                builder.AppendLine("* " + format(item));
            if (!string.IsNullOrWhiteSpace(conclusion))
            {
                builder.AppendLine();
                builder.AppendLine(conclusion);
            }

            builder.AppendLine();
        }
    }

    public static void Run(BuildContext context, DirectoryPath path,
        string currentVersion, string previousVersion, string lastCommit)
    {
        try
        {
            var config = new Config(currentVersion, previousVersion, lastCommit);
            var releaseNotes = MarkdownBuilder.Build(config).Result;
            context.GenerateFile(path.Combine($"v{config.CurrentVersion}.md").FullPath, releaseNotes, true);
        }
        catch (Exception e)
        {
            context.Error(e.ToString());
        }
    }
}