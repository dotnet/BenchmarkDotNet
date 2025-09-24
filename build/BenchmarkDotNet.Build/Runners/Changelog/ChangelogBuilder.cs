using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Options;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.FileHelpers;

namespace BenchmarkDotNet.Build.Runners.Changelog;

public class ChangelogBuilder
{
    private readonly BuildContext context;
    private readonly bool preview;
    private readonly string depth;
    private readonly bool forceClone;

    /// <summary>
    /// Directory with original changelog part files from branch 'docs-changelog'
    /// </summary>
    public DirectoryPath SrcDirectory { get; }

    /// <summary>
    /// Final changelog files to be used by docfx
    /// </summary>
    public DirectoryPath DocfxDirectory { get; }

    public ChangelogBuilder(BuildContext context)
    {
        this.context = context;
        preview = KnownOptions.DocsPreview.Resolve(context);
        depth = KnownOptions.DocsDepth.Resolve(context);
        forceClone = KnownOptions.ForceClone.Resolve(context);

        var docsDirectory = context.RootDirectory.Combine("docs");
        SrcDirectory = docsDirectory.Combine("_changelog");
        DocfxDirectory = docsDirectory.Combine("changelog");
    }

    public void Fetch()
    {
        EnvVar.GitHubToken.AssertHasValue();

        EnsureSrcDirectoryExist(forceClone);

        var history = context.VersionHistory;
        var stableVersionCount = history.StableVersions.Length;

        if (depth.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            FetchDetails(
                history.StableVersions.First(),
                history.FirstCommit);

            for (var i = 1; i < stableVersionCount; i++)
                FetchDetails(
                    history.StableVersions[i],
                    history.StableVersions[i - 1]);
        }
        else if (depth != "")
        {
            if (!int.TryParse(depth, CultureInfo.InvariantCulture, out var depthValue))
                throw new InvalidDataException($"Failed to parse the depth value: '{depth}'");

            for (var i = Math.Max(stableVersionCount - depthValue, 1); i < stableVersionCount; i++)
                FetchDetails(
                    history.StableVersions[i],
                    history.StableVersions[i - 1]);
        }

        if (preview)
            FetchDetails(
                history.CurrentVersion,
                history.StableVersions.Last(),
                "HEAD");
    }

    private void FetchDetails(string version, string versionPrevious, string lastCommit = "")
    {
        EnsureSrcDirectoryExist();
        context.Information($"Downloading changelog details for v{version}");
        var detailsDirectory = SrcDirectory.Combine("details");
        ChangelogDetailsBuilder.Run(context, detailsDirectory, version, versionPrevious, lastCommit);
    }

    public void Generate()
    {
        GenerateLastFooter();

        foreach (var version in context.VersionHistory.StableVersions)
            GenerateVersion(version);
        if (preview)
            GenerateVersion(context.VersionHistory.CurrentVersion);

        GenerateIndex();
        GenerateFull();
        GenerateToc();
    }

    public void GenerateLastFooter()
    {
        var version = context.VersionHistory.CurrentVersion;
        var previousVersion = context.VersionHistory.StableVersions.Last();
        var date = KnownOptions.Stable.Resolve(context)
            ? DateTime.Now.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)
            : "TBA";

        var content = new StringBuilder();
        content.AppendLine($"_Date: {date}_");
        content.AppendLine("");
        content.AppendLine(
            $"_Milestone: [v{version}](https://github.com/dotnet/BenchmarkDotNet/issues?q=milestone%3Av{version})_");
        content.AppendLine(
            $"([List of commits](https://github.com/dotnet/BenchmarkDotNet/compare/v{previousVersion}...v{version}))");
        content.AppendLine("");
        content.AppendLine("_NuGet Packages:_");
        foreach (var packageName in context.NuGetPackageNames)
            content.AppendLine($"* https://www.nuget.org/packages/{packageName}/{version}");

        var fileName = "v" + context.VersionHistory.CurrentVersion + ".md";
        var filePath = SrcDirectory.Combine("footer").CombineWithFilePath(fileName);
        context.GenerateFile(filePath, content);
    }

    private void GenerateVersion(string version)
    {
        EnsureSrcDirectoryExist();
        var md = $"v{version}.md";
        var header = SrcDirectory.Combine("header").CombineWithFilePath(md);
        var footer = SrcDirectory.Combine("footer").CombineWithFilePath(md);
        var details = SrcDirectory.Combine("details").CombineWithFilePath(md);
        var release = DocfxDirectory.CombineWithFilePath(md);

        var content = new StringBuilder();
        content.AppendLine("---");
        content.AppendLine("uid: changelog.v" + version);
        content.AppendLine("---");
        content.AppendLine("");
        content.AppendLine("# BenchmarkDotNet v" + version);
        content.AppendLine("");
        content.AppendLine("");

        if (context.FileExists(header))
        {
            content.AppendLine(context.FileReadText(header));
            content.AppendLine("");
            content.AppendLine("");
        }

        if (context.FileExists(details))
        {
            content.AppendLine(context.FileReadText(details));
            content.AppendLine("");
            content.AppendLine("");
        }

        if (context.FileExists(footer))
        {
            content.AppendLine("## Additional details");
            content.AppendLine("");
            content.AppendLine(context.FileReadText(footer));
        }

        context.GenerateFile(release, content.ToString());
    }

    private void GenerateIndex()
    {
        var content = new StringBuilder();
        content.AppendLine("---");
        content.AppendLine("uid: changelog");
        content.AppendLine("---");
        content.AppendLine("");
        content.AppendLine("# ChangeLog");
        content.AppendLine("");
        if (preview)
            content.AppendLine($"* @changelog.v{context.VersionHistory.CurrentVersion}");
        foreach (var version in context.VersionHistory.StableVersions.Reverse())
            content.AppendLine($"* @changelog.v{version}");
        content.AppendLine("* @changelog.full");

        context.GenerateFile(DocfxDirectory.CombineWithFilePath("index.md"), content);
    }

    private void GenerateFull()
    {
        var content = new StringBuilder();
        content.AppendLine("---");
        content.AppendLine("uid: changelog.full");
        content.AppendLine("---");
        content.AppendLine("");
        content.AppendLine("# Full ChangeLog");
        content.AppendLine("");
        if (preview)
            content.AppendLine(
                $"[!include[v{context.VersionHistory.CurrentVersion}](v{context.VersionHistory.CurrentVersion}.md)]");
        foreach (var version in context.VersionHistory.StableVersions.Reverse())
            content.AppendLine($"[!include[v{version}](v{version}.md)]");

        context.GenerateFile(DocfxDirectory.CombineWithFilePath("full.md"), content);
    }

    private void GenerateToc()
    {
        var content = new StringBuilder();

        if (preview)
        {
            content.AppendLine($"- name: v{context.VersionHistory.CurrentVersion}");
            content.AppendLine($"  href: v{context.VersionHistory.CurrentVersion}.md");
        }

        foreach (var version in context.VersionHistory.StableVersions.Reverse())
        {
            content.AppendLine($"- name: v{version}");
            content.AppendLine($"  href: v{version}.md");
        }

        content.AppendLine("- name: Full ChangeLog");
        content.AppendLine("  href: full.md");

        context.GenerateFile(DocfxDirectory.CombineWithFilePath("toc.yml"), content);
    }

    private void EnsureSrcDirectoryExist(bool forceClone = false)
    {
        void Log(string message) => context.Information($"[Changelog] {message}");

        Log($"Preparing git sub-repository for changelog branch '{Repo.ChangelogBranch}'. " +
            $"Target directory: '{SrcDirectory}'.");
        if (context.DirectoryExists(SrcDirectory) && forceClone)
        {
            Log($"Directory '{SrcDirectory}' already exists and forceClean is specified. " +
                $"Deleting the current directory...");
            context.DeleteDirectory(
                SrcDirectory,
                new DeleteDirectorySettings { Force = true, Recursive = true });
            Log($"Directory '{SrcDirectory}' deleted successfully.");
        }

        if (!context.DirectoryExists(SrcDirectory))
        {
            Log($"Cloning branch '{Repo.ChangelogBranch}' from '{Repo.HttpsGitUrl}' to '{SrcDirectory}'.");
            context.GitRunner.Clone(SrcDirectory, Repo.HttpsGitUrl, Repo.ChangelogBranch);
            Log($"Clone completed: '{Repo.ChangelogBranch}' -> '{SrcDirectory}'.");
        }
        else
        {
            Log($"Directory '{SrcDirectory}' already exists. Skipping clone.");
        }
    }
}