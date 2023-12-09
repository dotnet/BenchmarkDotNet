using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Build.Helpers;
using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Options;
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
        KnownOptions.Stable.AssertTrue(context);

        EnvVar.GitHubToken.AssertHasValue();
        if (KnownOptions.Push.Resolve(context))
            EnvVar.NuGetToken.AssertHasValue();
        else
            EnvVar.NuGetToken.SetEmpty();

        var currentVersion = context.VersionHistory.CurrentVersion;
        var tag = "v" + currentVersion;
        var nextVersion = KnownOptions.NextVersion.Resolve(context);
        if (nextVersion == "")
        {
            var version = Version.Parse(currentVersion);
            nextVersion = $"{version.Major}.{version.Minor}.{version.Build + 1}";
            context.Information($"Evaluated NextVersion: {nextVersion}");
        }

        context.GitRunner.Tag(tag);

        // Upgrade current version and commit changes
        UpdateVersionsTxt(nextVersion);
        UpdateCommonProps(nextVersion);
        context.Information($"Building {context.TemplatesTestsProjectFile}");
        context.BuildRunner.BuildProjectSilent(context.TemplatesTestsProjectFile);
        context.GitRunner.Commit($"Set next BenchmarkDotNet version: {nextVersion}");

        UpdateMilestones(nextVersion).Wait();

        context.GitRunner.BranchMove(Repo.DocsStableBranch, "HEAD");
        context.GitRunner.Push(Repo.MasterBranch);
        context.GitRunner.Push(Repo.DocsStableBranch, true);
        context.GitRunner.Push(tag);

        PushNupkg();

        PublishGitHubRelease();
    }

    private void UpdateVersionsTxt(string versionToAppend)
    {
        var content = context.FileReadText(context.VersionsFile).Trim();
        context.GenerateFile(context.VersionsFile, $"{content}\n{versionToAppend}");
    }

    private void UpdateCommonProps(string newCurrentVersion)
    {
        var content = Utils.ApplyRegex(
            context.FileReadText(context.CommonPropsFile),
            @"<VersionPrefix>([\d\.]+)</VersionPrefix>",
            newCurrentVersion);
        context.GenerateFile(context.CommonPropsFile, content);
    }

    private async Task UpdateMilestones(string nextVersion)
    {
        var currentVersion = context.VersionHistory.CurrentVersion;

        var client = Utils.CreateGitHubClient();
        var allMilestones = await client.Issue.Milestone.GetAllForRepository(Repo.Owner, Repo.Name);
        var currentMilestone = allMilestones.First(milestone => milestone.Title == $"v{currentVersion}");

        context.Information($"[GitHub] Close milestone v{currentVersion}");
        context.RunOnlyInPushMode(() =>
        {
            var milestoneUpdate = new MilestoneUpdate { State = ItemState.Closed, DueOn = DateTimeOffset.Now };
            client.Issue.Milestone.Update(Repo.Owner, Repo.Name, currentMilestone.Number, milestoneUpdate).Wait();
        });

        context.Information($"[GitHub] Create milestone v{nextVersion}");
        context.RunOnlyInPushMode(() =>
        {
            client.Issue.Milestone.Create(Repo.Owner, Repo.Name, new NewMilestone($"v{nextVersion}")).Wait();
        });
    }

    private void PushNupkg()
    {
        var nuGetToken = EnvVar.NuGetToken.GetValue();

        var files = context
            .GetFiles(context.ArtifactsDirectory.CombineWithFilePath("*.nupkg").FullPath)
            .OrderBy(file => file.FullPath);
        var settings = new DotNetNuGetPushSettings
        {
            ApiKey = nuGetToken,
            SymbolApiKey = nuGetToken,
            Source = "https://api.nuget.org/v3/index.json"
        };

        foreach (var file in files)
        {
            context.Information($"Push: {file}");
            context.RunOnlyInPushMode(() => context.DotNetNuGetPush(file, settings));
        }
    }

    private void PublishGitHubRelease()
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
        var client = Utils.CreateGitHubClient();
        context.RunOnlyInPushMode(() =>
        {
            client.Repository.Release.Create(Repo.Owner, Repo.Name, new NewRelease(tag)
            {
                Name = version,
                Draft = false,
                Prerelease = false,
                GenerateReleaseNotes = false,
                Body = notes
            }).Wait();
            context.Information("  Success");
        });
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