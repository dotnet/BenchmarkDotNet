using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Build.Helpers;
using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Options;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.FileHelpers;

namespace BenchmarkDotNet.Build.Runners;

public class DocumentationRunner
{
    private readonly BuildContext context;
    private readonly bool preview;
    private readonly string depth;

    public DirectoryPath ChangelogDirectory { get; }
    public DirectoryPath ChangelogSrcDirectory { get; }
    private readonly DirectoryPath changelogDetailsDirectory;
    private readonly DirectoryPath docsGeneratedDirectory;

    private readonly FilePath docfxJsonFile;
    private readonly FilePath redirectFile;
    private readonly FilePath readmeFile;
    private readonly FilePath rootIndexFile;
    private readonly FilePath changelogIndexFile;
    private readonly FilePath changelogFullFile;
    private readonly FilePath changelogTocFile;
    private readonly FilePath lastFooterFile;

    public DocumentationRunner(BuildContext context)
    {
        this.context = context;
        preview = KnownOptions.DocsPreview.Resolve(context);
        depth = KnownOptions.DocsDepth.Resolve(context);

        var docsDirectory = context.RootDirectory.Combine("docs");
        ChangelogDirectory = docsDirectory.Combine("changelog");
        ChangelogSrcDirectory = docsDirectory.Combine("_changelog");
        changelogDetailsDirectory = ChangelogSrcDirectory.Combine("details");
        docsGeneratedDirectory = docsDirectory.Combine("_site");

        redirectFile = docsDirectory.Combine("_redirects").CombineWithFilePath("_redirects");
        docfxJsonFile = docsDirectory.CombineWithFilePath("docfx.json");
        readmeFile = context.RootDirectory.CombineWithFilePath("README.md");
        rootIndexFile = docsDirectory.CombineWithFilePath("index.md");
        changelogIndexFile = ChangelogDirectory.CombineWithFilePath("index.md");
        changelogFullFile = ChangelogDirectory.CombineWithFilePath("full.md");
        changelogTocFile = ChangelogDirectory.CombineWithFilePath("toc.yml");
        lastFooterFile = ChangelogSrcDirectory.Combine("footer")
            .CombineWithFilePath("v" + context.VersionHistory.CurrentVersion + ".md");
    }

    public void Update()
    {
        EnvVar.GitHubToken.AssertHasValue();

        UpdateReadme();
        UpdateLastFooter();

        EnsureChangelogDetailsExist();

        var history = context.VersionHistory;
        var stableVersionCount = history.StableVersions.Length;

        if (depth.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            DocfxChangelogDownload(
                history.StableVersions.First(),
                history.FirstCommit);

            for (var i = 1; i < stableVersionCount; i++)
                DocfxChangelogDownload(
                    history.StableVersions[i],
                    history.StableVersions[i - 1]);
        }
        else if (depth != "")
        {
            if (!int.TryParse(depth, CultureInfo.InvariantCulture, out var depthValue))
                throw new InvalidDataException($"Failed to parse the depth value: '{depth}'");

            for (var i = Math.Max(stableVersionCount - depthValue, 1); i < stableVersionCount; i++)
                DocfxChangelogDownload(
                    history.StableVersions[i],
                    history.StableVersions[i - 1]);
        }

        if (preview)
            DocfxChangelogDownload(
                history.CurrentVersion,
                history.StableVersions.Last(),
                "HEAD");
    }

    private void UpdateReadme()
    {
        var content = Utils.ApplyRegex(
            context.FileReadText(context.ReadmeFile),
            @"\[(\d+)\+ GitHub projects\]",
            Repo.GetDependentProjectsNumber().Result.ToString()
        );

        context.GenerateFile(context.ReadmeFile, content, true);
    }

    public void Prepare()
    {
        foreach (var version in context.VersionHistory.StableVersions)
            DocfxChangelogGenerate(version);
        if (preview)
            DocfxChangelogGenerate(context.VersionHistory.CurrentVersion);

        GenerateIndexMd();
        GenerateChangelogIndex();
        GenerateChangelogFull();
        GenerateChangelogToc();
    }

    public void Build()
    {
        RunDocfx();
        GenerateRedirects();
    }

    private void RunDocfx()
    {
        context.Information($"Running docfx for '{docfxJsonFile}'");

        var currentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(docfxJsonFile.GetDirectory().FullPath);
        Docfx.Dotnet.DotnetApiCatalog.GenerateManagedReferenceYamlFiles(docfxJsonFile.FullPath).Wait();
        Docfx.Docset.Build(docfxJsonFile.FullPath).Wait();
        Directory.SetCurrentDirectory(currentDirectory);
    }

    private void GenerateIndexMd()
    {
        var content = new StringBuilder();
        content.AppendLine("---");
        content.AppendLine("title: Home");
        content.AppendLine("---");
        content.Append(context.FileReadText(readmeFile));

        context.GenerateFile(rootIndexFile, content);
    }

    private void GenerateChangelogToc()
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

        context.GenerateFile(changelogTocFile, content);
    }

    private void GenerateChangelogFull()
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

        context.GenerateFile(changelogFullFile, content);
    }

    private void GenerateChangelogIndex()
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

        context.GenerateFile(changelogIndexFile, content);
    }

    private void DocfxChangelogGenerate(string version)
    {
        EnsureChangelogDetailsExist();
        var md = $"v{version}.md";
        var header = ChangelogSrcDirectory.Combine("header").CombineWithFilePath(md);
        var footer = ChangelogSrcDirectory.Combine("footer").CombineWithFilePath(md);
        var details = ChangelogSrcDirectory.Combine("details").CombineWithFilePath(md);
        var release = ChangelogDirectory.CombineWithFilePath(md);

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

    private void EnsureChangelogDetailsExist(bool forceClean = false)
    {
        if (context.DirectoryExists(changelogDetailsDirectory) && forceClean)
            context.DeleteDirectory(
                changelogDetailsDirectory,
                new DeleteDirectorySettings { Force = true, Recursive = true });

        if (!context.DirectoryExists(changelogDetailsDirectory))
            context.GitRunner.Clone(changelogDetailsDirectory, Repo.HttpsGitUrl, Repo.ChangelogDetailsBranch);
    }

    private void DocfxChangelogDownload(string version, string versionPrevious, string lastCommit = "")
    {
        EnsureChangelogDetailsExist();
        context.Information($"Downloading changelog details for v{version}");
        ChangeLogBuilder.Run(context, changelogDetailsDirectory, version, versionPrevious, lastCommit);
    }

    private void GenerateRedirects()
    {
        if (!context.FileExists(redirectFile))
        {
            context.Error($"Redirect file '{redirectFile}' does not exist");
            return;
        }

        context.EnsureDirectoryExists(docsGeneratedDirectory);

        var redirects = context.FileReadLines(redirectFile)
            .Select(line => line.Split(' '))
            .Select(parts => (source: parts[0], target: parts[1]))
            .ToList();

        foreach (var (source, target) in redirects)
        {
            var fileName = source.StartsWith("/") || source.StartsWith("\\") ? source[1..] : source;
            var fullFilePath = docsGeneratedDirectory.CombineWithFilePath(fileName);
            var content =
                $"<!doctype html>" +
                $"<html lang=en-us>" +
                $"<head>" +
                $"<title>{target}</title>" +
                $"<link rel=canonical href='{target}'>" +
                $"<meta name=robots content=\"noindex\">" +
                $"<meta charset=utf-8><meta http-equiv=refresh content=\"0; url={target}\">" +
                $"</head>" +
                $"</html>";
            context.EnsureDirectoryExists(fullFilePath.GetDirectory());
            context.GenerateFile(fullFilePath, content);
        }
    }

    private void UpdateLastFooter()
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

        context.GenerateFile(lastFooterFile, content);
    }
}