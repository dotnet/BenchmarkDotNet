using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Build.Meta;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.FileHelpers;
using Octokit;

namespace BenchmarkDotNet.Build.Runners;

public class ReleaseRunner
{
    private readonly BuildContext context;

    public ReleaseRunner(BuildContext context)
    {
        this.context = context;
    }

    public void Run()
    {
        var nextVersion = context.NextVersion;
        var currentVersion = context.VersionHistory.CurrentVersion;
        var isStable = context.VersionStable;
        var tag = "v" + currentVersion;

        if (string.IsNullOrEmpty(nextVersion))
            throw new Exception("NextVersion is not specified");
        if (!isStable)
            throw new Exception("VersionStable is not specified");
        if (string.IsNullOrEmpty(GitHubCredentials.Token))
            throw new Exception($"Environment variable '{GitHubCredentials.TokenVariableName}' is not specified!");
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NUGET_TOKEN")))
            throw new Exception($"Environment variable 'NUGET_TOKEN' is not specified!");

        context.GitRunner.Tag(tag);
        context.GitRunner.BranchMove(Repo.DocsStableBranch, "HEAD");
        
        // Upgrade current version and commit changes
        UpdateVersionsTxt();
        UpdateCommonProps();
        context.Information($"Building {context.TemplatesTestsProjectFile}");
        context.BuildRunner.BuildProjectSilent(context.TemplatesTestsProjectFile);
        context.GitRunner.Commit($"Set next BenchmarkDotNet version: {nextVersion}");
        
        UpdateMilestones().Wait();
        
        context.GitRunner.Push(Repo.MasterBranch);
        context.GitRunner.Push(Repo.DocsStableBranch, true);
        context.GitRunner.Push(tag);

        PushNupkg();

        PublishGitHubRelease().Wait();
    }

    private void UpdateVersionsTxt()
    {
        var content = context.FileReadText(context.VersionsFile).Trim();
        context.GenerateFile(context.VersionsFile, $"{content}\n{context.NextVersion}");
    }

    private void UpdateCommonProps()
    {
        var regex = new Regex(@"<VersionPrefix>([\d\.]+)</VersionPrefix>");

        var content = context.FileReadText(context.CommonPropsFile);
        var match = regex.Match(content);
        if (!match.Success)
            throw new Exception($"Failed to find VersionPrefix definition in {context.CommonPropsFile}");

        var oldVersion = match.Groups[1].Value;
        context.GenerateFile(context.CommonPropsFile, content.Replace(oldVersion, context.NextVersion));
    }

    private async Task UpdateMilestones()
    {
        var currentVersion = context.VersionHistory.CurrentVersion;
        var nextVersion = context.NextVersion;

        var client = GitHubCredentials.CreateClient();
        var allMilestones = await client.Issue.Milestone.GetAllForRepository(Repo.Owner, Repo.Name);
        var currentMilestone = allMilestones.First(milestone => milestone.Title == $"v{currentVersion}");

        context.Information($"[GitHub] Close milestone v{currentVersion}");
        if (context.PushMode)
        {
            await client.Issue.Milestone.Update(Repo.Owner, Repo.Name, currentMilestone.Number,
                new MilestoneUpdate { State = ItemState.Closed, DueOn = DateTimeOffset.Now });
        }
        else
        {
            context.Information("  Skip because PushMode is disabled");
        }

        context.Information($"[GitHub] Create milestone v{nextVersion}");
        if (context.PushMode)
        {
            await client.Issue.Milestone.Create(Repo.Owner, Repo.Name, new NewMilestone($"v{nextVersion}"));
        }
        else
        {
            context.Information("  Skip because PushMode is disabled");
        }
    }

    private void PushNupkg()
    {
        var nuGetToken = Environment.GetEnvironmentVariable("NUGET_TOKEN");

        var files = context
            .GetFiles(context.ArtifactsDirectory.CombineWithFilePath("*").FullPath)
            .OrderBy(file => file.FullPath);
        var settings = new DotNetNuGetPushSettings
        {
            ApiKey = nuGetToken,
            SymbolApiKey = nuGetToken
        };

        foreach (var file in files)
        {
            context.Information($"Push: {file}");
            if (context.PushMode)
                context.DotNetNuGetPush(file, settings);
            else
                context.Information("  Skip because PushMode is disabled");
        }
    }

    private async Task PublishGitHubRelease()
    {
        var version = context.VersionHistory.CurrentVersion;
        var tag = $"v{version}";
        var notesFile = context.DocumentationRunner
            .ChangelogSrcDirectory
            .Combine("header")
            .CombineWithFilePath($"{tag}.md");
        var notes = $"Full changelog: https://benchmarkdotnet.org/changelog/{tag}.html\n\n" +
                    PreprocessMarkdown(context.FileReadText(notesFile));

        context.Information($"[GitHub] Creating release '{version}'");
        var client = GitHubCredentials.CreateClient();
        if (context.PushMode)
        {
            await client.Repository.Release.Create(Repo.Owner, Repo.Name, new NewRelease(tag)
            {
                Name = version,
                Draft = false,
                Prerelease = false,
                GenerateReleaseNotes = false,
                Body = notes
            });
            context.Information("  Success");
        }
        else
        {
            context.Information("  Skip because PushMode is disabled");
        }
    }

    private static string PreprocessMarkdown(string content)
    {
        var lines = content.Split("\n");
        var newContent = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            newContent.Append(lines[i]);
            if (i == lines.Length - 1)
                continue;
            if (!lines[i].EndsWith("  ") && lines[i + 1].StartsWith("  "))
                continue;
            newContent.Append("\n");
        }

        return newContent.ToString();
    }
}