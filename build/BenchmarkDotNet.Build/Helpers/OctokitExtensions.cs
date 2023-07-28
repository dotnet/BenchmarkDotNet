using System;
using System.Linq;
using Octokit;

namespace BenchmarkDotNet.Build.Helpers;

public static class OctokitExtensions
{
    public static string ToStr(this User? user, string prefix) => user != null
        ? $" ({prefix} [@{user.Login}]({user.HtmlUrl}))"
        : "";

    private static string ToStr(this Author? user, string prefix) => user != null
        ? $" ({prefix} {user.ToLink()})"
        : "";

    private static string ToStr(this Committer? user, string prefix) => user != null
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