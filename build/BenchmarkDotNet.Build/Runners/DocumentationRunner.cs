using System;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Build.Meta;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.FileHelpers;

namespace BenchmarkDotNet.Build.Runners;

public class DocumentationRunner
{
    private readonly BuildContext context;

    public DocumentationRunner(BuildContext context)
    {
        this.context = context;
    }

    private void GenerateRedirects()
    {
        var redirectFile = context.RedirectRootDirectory.CombineWithFilePath("_redirects");
        if (!context.FileExists(redirectFile))
        {
            context.Error($"Redirect file '{redirectFile}' does not exist");
            return;
        }

        context.EnsureDirectoryExists(context.RedirectTargetDirectory);

        var redirects = context.FileReadLines(redirectFile)
            .Select(line => line.Split(' '))
            .Select(parts => (source: parts[0], target: parts[1]))
            .ToList();

        foreach (var (source, target) in redirects)
        {
            var fileName = source.StartsWith("/") || source.StartsWith("\\") ? source[1..] : source;
            var fullFileName = context.RedirectTargetDirectory.CombineWithFilePath(fileName);
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
            context.EnsureDirectoryExists(fullFileName.GetDirectory());
            context.FileWriteText(fullFileName, content);
        }
    }

    private void RunDocfx(FilePath docfxJson)
    {
        context.Information($"Running docfx for '{docfxJson}'");

        var currentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(docfxJson.GetDirectory().FullPath);
        Microsoft.DocAsCode.Dotnet.DotnetApiCatalog.GenerateManagedReferenceYamlFiles(docfxJson.FullPath).Wait();
        Microsoft.DocAsCode.Docset.Build(docfxJson.FullPath).Wait();
        Directory.SetCurrentDirectory(currentDirectory);
    }

    private void GenerateIndexMd()
    {
        context.Information("DocsBuild: Generate index.md");
        var content = new StringBuilder();
        content.AppendLine("---");
        content.AppendLine("title: Home");
        content.AppendLine("---");
        content.Append(context.FileReadText(context.RootDirectory.CombineWithFilePath("README.md")));
        context.FileWriteText(context.DocsDirectory.CombineWithFilePath("index.md"), content.ToString());
    }

    public void Update()
    {
        context.EnsureChangelogDetailsExist();

        ReadmeUpdater.Run(context);

        if (string.IsNullOrEmpty(Repo.ProductHeader))
            throw new Exception($"Environment variable '{Repo.ProductHeaderVar}' is not specified!");
        if (string.IsNullOrEmpty(Repo.Token))
            throw new Exception($"Environment variable '{Repo.TokenVar}' is not specified!");

        var history = context.VersionHistory; 

        var depth = context.Depth;
        var stableVersionCount = history.StableVersions.Length;

        if (depth == 0)
        {
            context.DocfxChangelogDownload(
                history.StableVersions.First(),
                history.FirstCommit);

            for (int i = 1; i < stableVersionCount; i++)
                context.DocfxChangelogDownload(
                    history.StableVersions[i],
                    history.StableVersions[i - 1]);
        }
        else if (depth > 0)
        {
            for (int i = Math.Max(stableVersionCount - depth, 1); i < stableVersionCount; i++)
                context.DocfxChangelogDownload(
                    history.StableVersions[i],
                    history.StableVersions[i - 1]);
        }

        context.DocfxChangelogDownload(
            history.NextVersion,
            history.StableVersions.Last(),
            "HEAD");
    }

    public void Prepare()
    {
        var history = context.VersionHistory;
        
        foreach (var version in history.StableVersions)
            context.DocfxChangelogGenerate(version);
        context.DocfxChangelogGenerate(history.NextVersion);

        context.Information("DocfxChangelogGenerate: index.md");
        var indexContent = new StringBuilder();
        indexContent.AppendLine("---");
        indexContent.AppendLine("uid: changelog");
        indexContent.AppendLine("---");
        indexContent.AppendLine("");
        indexContent.AppendLine("# ChangeLog");
        indexContent.AppendLine("");
        foreach (var version in history.StableVersions.Reverse())
            indexContent.AppendLine($"* @changelog.{version}");
        indexContent.AppendLine("* @changelog.full");
        context.FileWriteText(context.ChangeLogDirectory.CombineWithFilePath("index.md"), indexContent.ToString());

        context.Information("DocfxChangelogGenerate: full.md");
        var fullContent = new StringBuilder();
        fullContent.AppendLine("---");
        fullContent.AppendLine("uid: changelog.full");
        fullContent.AppendLine("---");
        fullContent.AppendLine("");
        fullContent.AppendLine("# Full ChangeLog");
        fullContent.AppendLine("");
        foreach (var version in history.StableVersions.Reverse())
            fullContent.AppendLine($"[!include[{version}]({version}.md)]");
        context.FileWriteText(context.ChangeLogDirectory.CombineWithFilePath("full.md"), fullContent.ToString());

        context.Information("DocfxChangelogGenerate: toc.yml");
        var tocContent = new StringBuilder();
        foreach (var version in history.StableVersions.Reverse())
        {
            tocContent.AppendLine($"- name: {version}");
            tocContent.AppendLine($"  href: {version}.md");
        }

        tocContent.AppendLine("- name: Full ChangeLog");
        tocContent.AppendLine("  href: full.md");
        context.FileWriteText(context.ChangeLogDirectory.CombineWithFilePath("toc.yml"), tocContent.ToString());
    }

    public void Build()
    {
        GenerateIndexMd();
        RunDocfx(context.DocfxJsonFile);
        GenerateRedirects();
    }
}