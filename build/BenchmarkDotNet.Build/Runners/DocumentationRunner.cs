using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Build.Helpers;
using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Runners.Changelog;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.FileHelpers;

namespace BenchmarkDotNet.Build.Runners;

public class DocumentationRunner
{
    private readonly BuildContext context;
    private readonly ChangelogBuilder changelogBuilder;
    private readonly DirectoryPath docsGeneratedDirectory;

    private readonly FilePath docfxJsonFile;
    private readonly FilePath redirectFile;
    private readonly FilePath readmeFile;
    private readonly FilePath rootIndexFile;

    public DirectoryPath ChangelogSrcDirectory => changelogBuilder.SrcDirectory;

    public DocumentationRunner(BuildContext context)
    {
        this.context = context;
        changelogBuilder = new ChangelogBuilder(context);

        var docsDirectory = context.RootDirectory.Combine("docs");
        docsGeneratedDirectory = docsDirectory.Combine("_site");
        redirectFile = docsDirectory.Combine("_redirects").CombineWithFilePath("_redirects");
        docfxJsonFile = docsDirectory.CombineWithFilePath("docfx.json");
        readmeFile = context.RootDirectory.CombineWithFilePath("README.md");
        rootIndexFile = docsDirectory.CombineWithFilePath("index.md");
    }

    public void Fetch()
    {
        EnvVar.GitHubToken.AssertHasValue();
        changelogBuilder.Fetch();
    }

    public void Generate()
    {
        changelogBuilder.Generate();

        UpdateReadme();
        GenerateIndexMd();
    }

    public void Build()
    {
        RunDocfx();
        GenerateRedirects();
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
}