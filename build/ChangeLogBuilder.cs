using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.IO;
using JetBrains.Annotations;
using Octokit;

namespace Build;

public static class OctokitExtensions
{
    public static string ToStr(this User user, string prefix) => user != null
        ? $" ({prefix} [@{user.Login}]({user.HtmlUrl}))"
        : "";

    private static string ToStr(this Author user, string prefix) => user != null
        ? $" ({prefix} {user.ToLink()})"
        : "";

    private static string ToStr(this Committer user, string prefix) => user != null
        ? $" ({prefix} {user.Name})"
        : "";

    public static string ToLink(this Author user) => $"[@{user.Login}]({user.HtmlUrl})";

    public static string ToLinkWithName(this Author user, string name) => $"[@{user.Login} ({name})]({user.HtmlUrl})";

    public static string ToCommitMessage(this Commit commit)
    {
        var message = commit.Message.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "";
        return message.Length > 80 ? message.Substring(0, 77) + "..." : message;
    }

    public static string ToLink(this GitHubCommit commit) => $"[{commit.Sha.Substring(0, 6)}]({commit.HtmlUrl})";

    public static string ToByStr(this GitHubCommit commit)
    {
        if (commit.Author != null)
            return commit.Author.ToStr("by");
        return commit.Commit.Author != null ? commit.Commit.Author.ToStr("by") : "";
    }
}

public class ChangeLogBuilder
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

        public Config(string currentMilestone, string previousMilestone, string lastCommit)
        {
            CurrentMilestone = currentMilestone;
            PreviousMilestone = previousMilestone;
            LastCommit = lastCommit;
        }
    }

    public class AuthorEqualityComparer : IEqualityComparer<Author>
    {
        public static readonly IEqualityComparer<Author> Default = new AuthorEqualityComparer();

        public bool Equals(Author x, Author y) => x.Login == y.Login;

        public int GetHashCode(Author author) => author.Login.GetHashCode();
    }

    public class MarkdownBuilder
    {
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
            var (repoOwner, repoName, milestone, previousMilestone, lastCommit) = config;

            var client = new GitHubClient(new ProductHeaderValue(config.ProductHeader));
            var tokenAuth = new Credentials(config.Token);
            client.Credentials = tokenAuth;

            if (milestone == "_")
            {
                var allContributors = await client.Repository.GetAllContributors(repoOwner, repoName);
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

            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Closed
            };
            var pullRequestRequest = new PullRequestRequest
            {
                State = ItemStateFilter.Closed
            };

            var issues = (await client.Issue.GetAllForRepository(repoOwner, repoName, issueRequest))
                .Where(issue => issue.Milestone != null && issue.Milestone.Title == milestone)
                .Where(issue => issue.PullRequest == null)
                .OrderBy(issue => issue.Number)
                .ToList();

            var pullRequests =
                (await client.PullRequest.GetAllForRepository(repoOwner, repoName, pullRequestRequest))
                .Where(issue => issue.Milestone != null && issue.Milestone.Title == milestone)
                .OrderBy(issue => issue.Number)
                .ToList();

            var compare = await client.Repository.Commit.Compare(repoOwner, repoName, previousMilestone, lastCommit);
            var commits = compare.Commits;

            var authorNames = new Dictionary<string, string>();
            foreach (var contributor in commits.Select(commit => commit.Author))
                if (contributor != null && !authorNames.ContainsKey(contributor.Login))
                {
                    var user = await client.User.Get(contributor.Login);
                    var name = user?.Name;
                    authorNames[contributor.Login] = string.IsNullOrWhiteSpace(name) ? contributor.Login : name;
                }

            var contributors = compare.Commits
                .Select(commit => commit.Author)
                .Where(author => author != null)
                .Distinct(AuthorEqualityComparer.Default)
                .OrderBy(author => authorNames[author.Login])
                .ToImmutableList();

            var milestoneHtmlUlr = $"https://github.com/{repoOwner}/{repoName}/issues?q=milestone:{milestone}";

            builder.AppendLine("## Milestone details");
            builder.AppendLine();
            builder.AppendLine($"In the [{milestone}]({milestoneHtmlUlr}) scope, ");
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
            AppendList("Contributors", contributors, contributor =>
                    $"{authorNames[contributor.Login]} ({contributor.ToLink()})".Trim(),
                "Thank you very much!");

            return builder.ToString();
        }

        private void AppendList<T>(string title, IReadOnlyList<T> items, Func<T, string> format,
            string conclusion = null)
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
    
    public static async Task Run(DirectoryPath path, string currentMilestone, string previousMilestone, string lastCommit)
    {
        try
        {
            var config = new Config(currentMilestone, previousMilestone, lastCommit);
            var releaseNotes = await MarkdownBuilder.Build(config);
            await File.WriteAllTextAsync(path.Combine(config.CurrentMilestone + ".md").FullPath, releaseNotes);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Demystify().ToString());
        }
    }
}