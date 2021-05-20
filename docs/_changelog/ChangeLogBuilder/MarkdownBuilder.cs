using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ChangeLogBuilder
{
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
}
