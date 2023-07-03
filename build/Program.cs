using System.IO;
using System.Linq;
using System.Text;
using Build;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Build.AppVeyor;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core;
using Cake.Core.IO;
using Cake.FileHelpers;
using Cake.Frosting;
using Cake.Git;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string BuildConfiguration { get; set; }
    public bool SkipTests { get; set; }
    public bool SkipSlowTests { get; set; }
    public string TargetVersion { get; set; }

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath ArtifactsDirectory { get; }
    public DirectoryPath ToolsDirectory { get; }
    public DirectoryPath DocsDirectory { get; }
    public FilePath DocfxJsonFile { get; }
    public DirectoryPath TestOutputDirectory { get; }

    public DirectoryPath ChangeLogDirectory { get; }
    public DirectoryPath ChangeLogGenDirectory { get; }

    public DirectoryPath RedirectRootDirectory { get; }
    public DirectoryPath RedirectTargetDirectory { get; }

    public FilePath SolutionFile { get; }
    public FilePath UnitTestsProjectFile { get; }
    public FilePath IntegrationTestsProjectFile { get; }
    public FilePath TemplatesTestsProjectFile { get; }
    public FilePathCollection AllPackableSrcProjects { get; }

    public DotNetMSBuildSettings MsBuildSettingsRestore { get; }
    public DotNetMSBuildSettings MsBuildSettingsBuild { get; }
    public DotNetMSBuildSettings MsBuildSettingsPack { get; }

    private IAppVeyorProvider AppVeyor => this.BuildSystem().AppVeyor;
    public bool IsRunningOnAppVeyor => AppVeyor.IsRunningOnAppVeyor;
    public bool IsOnAppVeyorAndNotPr => IsRunningOnAppVeyor && !AppVeyor.Environment.PullRequest.IsPullRequest;

    public bool IsOnAppVeyorAndBdnNightlyCiCd => IsOnAppVeyorAndNotPr &&
                                                 AppVeyor.Environment.Repository.Branch == "master" &&
                                                 this.IsRunningOnWindows();

    public bool IsLocalBuild => this.BuildSystem().IsLocalBuild;
    public bool IsCiBuild => !this.BuildSystem().IsLocalBuild;

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("Configuration", "Release");
        SkipTests = context.Argument("SkipTests", false);
        SkipSlowTests = context.Argument("SkipSlowTests", false);
        TargetVersion = context.Argument("Version", "");

        RootDirectory = new DirectoryPath(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName);
        ArtifactsDirectory = RootDirectory.Combine("artifacts");
        ToolsDirectory = RootDirectory.Combine("tools");
        DocsDirectory = RootDirectory.Combine("docs");
        DocfxJsonFile = DocsDirectory.CombineWithFilePath("docfx.json");
        TestOutputDirectory = RootDirectory.Combine("TestResults");

        ChangeLogDirectory = RootDirectory.Combine("docs").Combine("changelog");
        ChangeLogGenDirectory = RootDirectory.Combine("docs").Combine("_changelog");

        RedirectRootDirectory = RootDirectory.Combine("docs").Combine("_redirects");
        RedirectTargetDirectory = RootDirectory.Combine("docs").Combine("_site");

        SolutionFile = RootDirectory.CombineWithFilePath("BenchmarkDotNet.sln");
        UnitTestsProjectFile = RootDirectory.Combine("tests").Combine("BenchmarkDotNet.Tests")
            .CombineWithFilePath("BenchmarkDotNet.Tests.csproj");
        IntegrationTestsProjectFile = RootDirectory.Combine("tests").Combine("BenchmarkDotNet.IntegrationTests")
            .CombineWithFilePath("BenchmarkDotNet.IntegrationTests.csproj");
        TemplatesTestsProjectFile = RootDirectory.Combine("templates")
            .CombineWithFilePath("BenchmarkDotNet.Templates.csproj");
        AllPackableSrcProjects = new FilePathCollection(context.GetFiles(RootDirectory.FullPath + "/src/**/*.csproj")
            .Where(p => !p.FullPath.Contains("Disassembler")));

        MsBuildSettingsRestore = new DotNetMSBuildSettings();
        MsBuildSettingsBuild = new DotNetMSBuildSettings();
        MsBuildSettingsPack = new DotNetMSBuildSettings();

        if (IsCiBuild)
        {
            System.Environment.SetEnvironmentVariable("BDN_CI_BUILD", "true");

            MsBuildSettingsBuild.MaxCpuCount = 1;
            MsBuildSettingsBuild.WithProperty("UseSharedCompilation", "false");
        }

        if (!string.IsNullOrEmpty(TargetVersion))
        {
            MsBuildSettingsRestore.WithProperty("Version", TargetVersion);
            MsBuildSettingsBuild.WithProperty("Version", TargetVersion);
            MsBuildSettingsPack.WithProperty("Version", TargetVersion);
        }

        // NativeAOT build requires VS C++ tools to be added to $path via vcvars64.bat
        // but once we do that, dotnet restore fails with:
        // "Please specify a valid solution configuration using the Configuration and Platform properties"
        if (context.IsRunningOnWindows())
        {
            MsBuildSettingsRestore.WithProperty("Platform", "Any CPU");
            MsBuildSettingsBuild.WithProperty("Platform", "Any CPU");
        }
    }

    private DotNetTestSettings GetTestSettingsParameters(FilePath logFile, string tfm)
    {
        var settings = new DotNetTestSettings
        {
            Configuration = BuildConfiguration,
            Framework = tfm,
            NoBuild = true,
            NoRestore = true,
            Loggers = new[] { "trx", $"trx;LogFileName={logFile.FullPath}", "console;verbosity=detailed" }
        };
        // force the tool to not look for the .dll in platform-specific directory
        settings.EnvironmentVariables["Platform"] = "";
        return settings;
    }

    public void RunTests(FilePath projectFile, string alias, string tfm)
    {
        var xUnitXmlFile = TestOutputDirectory.CombineWithFilePath(alias + "-" + tfm + ".trx");
        this.Information($"Run tests for {projectFile} ({tfm}), result file: '{xUnitXmlFile}'");
        var settings = GetTestSettingsParameters(xUnitXmlFile, tfm);
        this.DotNetTest(projectFile.FullPath, settings);
    }

    public void EnsureChangelogDetailsExist(bool forceClean = false)
    {
        var path = ChangeLogGenDirectory.Combine("details");
        if (this.DirectoryExists(path) && forceClean)
            this.DeleteDirectory(path, new DeleteDirectorySettings() { Force = true, Recursive = true });
        
        if (!this.DirectoryExists(path))
        {
            var settings = new GitCloneSettings { Checkout = true, BranchName = "docs-changelog-details" };
            this.GitClone("https://github.com/dotnet/BenchmarkDotNet.git", path, settings);
        }
    }

    public void DocfxChangelogDownload(string version, string versionPrevious, string lastCommit = "")
    {
        EnsureChangelogDetailsExist(true);
        this.Information("DocfxChangelogDownload: " + version);
        // Required environment variables: GITHUB_PRODUCT, GITHUB_TOKEN
        var path = ChangeLogGenDirectory.Combine("details");
        ChangeLogBuilder.Run(path, version, versionPrevious, lastCommit).Wait();
    }

    public void DocfxChangelogGenerate(string version)
    {
        EnsureChangelogDetailsExist();
        this.Information("DocfxChangelogGenerate: " + version);
        var header = ChangeLogGenDirectory.Combine("header").CombineWithFilePath(version + ".md");
        var footer = ChangeLogGenDirectory.Combine("footer").CombineWithFilePath(version + ".md");
        var details = ChangeLogGenDirectory.Combine("details").CombineWithFilePath(version + ".md");
        var release = ChangeLogDirectory.CombineWithFilePath(version + ".md");

        var content = new StringBuilder();
        content.AppendLine("---");
        content.AppendLine("uid: changelog." + version);
        content.AppendLine("---");
        content.AppendLine("");
        content.AppendLine("# BenchmarkDotNet " + version);
        content.AppendLine("");
        content.AppendLine("");

        if (this.FileExists(header))
        {
            content.AppendLine(this.FileReadText(header));
            content.AppendLine("");
            content.AppendLine("");
        }

        if (this.FileExists(details))
        {
            content.AppendLine(this.FileReadText(details));
            content.AppendLine("");
            content.AppendLine("");
        }

        if (this.FileExists(footer))
        {
            content.AppendLine("## Additional details");
            content.AppendLine("");
            content.AppendLine(this.FileReadText(footer));
        }

        this.FileWriteText(release, content.ToString());
    }

    public void RunDocfx(FilePath docfxJson)
    {
        this.Information($"Running docfx for '{docfxJson}'");

        var currentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(docfxJson.GetDirectory().FullPath);
        Microsoft.DocAsCode.Dotnet.DotnetApiCatalog.GenerateManagedReferenceYamlFiles(docfxJson.FullPath).Wait();
        Microsoft.DocAsCode.Docset.Build(docfxJson.FullPath).Wait();
        Directory.SetCurrentDirectory(currentDirectory);
    }

    public void GenerateRedirects()
    {
        var redirectFile = RedirectRootDirectory.CombineWithFilePath("_redirects");
        if (!this.FileExists(redirectFile))
        {
            this.Error($"Redirect file '{redirectFile}' does not exist");
            return;
        }

        this.EnsureDirectoryExists(RedirectTargetDirectory);

        var redirects = this.FileReadLines(redirectFile)
            .Select(line => line.Split(' '))
            .Select(parts => (source: parts[0], target: parts[1]))
            .ToList();

        foreach (var (source, target) in redirects)
        {
            var fileName = source.StartsWith("/") || source.StartsWith("\\") ? source[1..] : source;
            var fullFileName = RedirectTargetDirectory.CombineWithFilePath(fileName);
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
            this.EnsureDirectoryExists(fullFileName.GetDirectory());
            this.FileWriteText(fullFileName, content);
        }
    }
}

public static class DocumentationHelper
{
    public static readonly string[] BdnAllVersions =
    {
        "v0.7.0",
        "v0.7.1",
        "v0.7.2",
        "v0.7.3",
        "v0.7.4",
        "v0.7.5",
        "v0.7.6",
        "v0.7.7",
        "v0.7.8",
        "v0.8.0",
        "v0.8.1",
        "v0.8.2",
        "v0.9.0",
        "v0.9.1",
        "v0.9.2",
        "v0.9.3",
        "v0.9.4",
        "v0.9.5",
        "v0.9.6",
        "v0.9.7",
        "v0.9.8",
        "v0.9.9",
        "v0.10.0",
        "v0.10.1",
        "v0.10.2",
        "v0.10.3",
        "v0.10.4",
        "v0.10.5",
        "v0.10.6",
        "v0.10.7",
        "v0.10.8",
        "v0.10.9",
        "v0.10.10",
        "v0.10.11",
        "v0.10.12",
        "v0.10.13",
        "v0.10.14",
        "v0.11.0",
        "v0.11.1",
        "v0.11.2",
        "v0.11.3",
        "v0.11.4",
        "v0.11.5",
        "v0.12.0",
        "v0.12.1",
        "v0.13.0",
        "v0.13.1",
        "v0.13.2",
        "v0.13.3",
        "v0.13.4",
        "v0.13.5"
    };

    public const string BdnNextVersion = "v0.13.6";
    public const string BdnFirstCommit = "6eda98ab1e83a0d185d09ff8b24c795711af8db1";
}

[TaskName("Clean")]
public class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.CleanDirectory(context.ArtifactsDirectory);
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore(context.SolutionFile.FullPath,
            new DotNetRestoreSettings
            {
                MSBuildSettings = context.MsBuildSettingsRestore
            });
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
public class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("BuildSystemProvider: " + context.BuildSystem().Provider);
        context.DotNetBuild(context.SolutionFile.FullPath, new DotNetBuildSettings
        {
            Configuration = context.BuildConfiguration,
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Verbosity = DotNetVerbosity.Minimal
        });
    }
}

[TaskName("FastTests")]
[IsDependentOn(typeof(BuildTask))]
public class FastTestsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return !context.SkipTests;
    }

    public override void Run(BuildContext context)
    {
        var targetFrameworks = context.IsRunningOnWindows()
            ? new[] { "net462", "net7.0" }
            : new[] { "net7.0" };

        foreach (var targetFramework in targetFrameworks)
            context.RunTests(context.UnitTestsProjectFile, "UnitTests", targetFramework);
    }
}

[TaskName("SlowFullFrameworkTests")]
[IsDependentOn(typeof(BuildTask))]
public class SlowFullFrameworkTestsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return !context.SkipTests && !context.SkipSlowTests && context.IsRunningOnWindows() &&
               !context.IsRunningOnAppVeyor;
    }

    public override void Run(BuildContext context)
    {
        context.RunTests(context.IntegrationTestsProjectFile, "IntegrationTests", "net462");
    }
}

[TaskName("SlowTestsNetCore")]
[IsDependentOn(typeof(BuildTask))]
public class SlowTestsNetCoreTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return !context.SkipTests && !context.SkipSlowTests;
    }

    public override void Run(BuildContext context)
    {
        context.RunTests(context.IntegrationTestsProjectFile, "IntegrationTests", "net7.0");
    }
}

[TaskName("AllTests")]
[IsDependentOn(typeof(FastTestsTask))]
[IsDependentOn(typeof(SlowFullFrameworkTestsTask))]
[IsDependentOn(typeof(SlowTestsNetCoreTask))]
public class AllTestsTask : FrostingTask<BuildContext>
{
}

[TaskName("Pack")]
[IsDependentOn(typeof(BuildTask))]
public class PackTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.IsOnAppVeyorAndBdnNightlyCiCd || context.IsLocalBuild;
    }

    public override void Run(BuildContext context)
    {
        var settingsSrc = new DotNetPackSettings
        {
            Configuration = context.BuildConfiguration,
            OutputDirectory = context.ArtifactsDirectory.FullPath,
            ArgumentCustomization = args => args.Append("--include-symbols").Append("-p:SymbolPackageFormat=snupkg"),
            MSBuildSettings = context.MsBuildSettingsPack
        };

        foreach (var project in context.AllPackableSrcProjects)
            context.DotNetPack(project.FullPath, settingsSrc);

        var settingsTemplate = new DotNetPackSettings
        {
            Configuration = context.BuildConfiguration,
            OutputDirectory = context.ArtifactsDirectory.FullPath
        };
        context.DotNetPack(context.TemplatesTestsProjectFile.FullPath, settingsTemplate);
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(AllTestsTask))]
[IsDependentOn(typeof(PackTask))]
public class DefaultTask : FrostingTask
{
}


[TaskName("DocFX_Changelog_Download")]
public class DocFxChangelogDownloadTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.Argument("AllVersions", false))
        {
            context.DocfxChangelogDownload(
                DocumentationHelper.BdnAllVersions.First(),
                DocumentationHelper.BdnFirstCommit);

            for (int i = 1; i < DocumentationHelper.BdnAllVersions.Length; i++)
                context.DocfxChangelogDownload(
                    DocumentationHelper.BdnAllVersions[i],
                    DocumentationHelper.BdnAllVersions[i - 1]);
        }
        else if (context.Argument("LatestVersions", false))
        {
            for (int i = DocumentationHelper.BdnAllVersions.Length - 3;
                 i < DocumentationHelper.BdnAllVersions.Length;
                 i++)
                context.DocfxChangelogDownload(
                    DocumentationHelper.BdnAllVersions[i],
                    DocumentationHelper.BdnAllVersions[i - 1]);
        }

        context.DocfxChangelogDownload(
            DocumentationHelper.BdnNextVersion,
            DocumentationHelper.BdnAllVersions.Last(),
            "HEAD");
    }
}

[TaskName("DocFX_Changelog_Generate")]
public class DocfxChangelogGenerateTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var version in DocumentationHelper.BdnAllVersions)
            context.DocfxChangelogGenerate(version);
        context.DocfxChangelogGenerate(DocumentationHelper.BdnNextVersion);

        context.Information("DocfxChangelogGenerate: index.md");
        var indexContent = new StringBuilder();
        indexContent.AppendLine("---");
        indexContent.AppendLine("uid: changelog");
        indexContent.AppendLine("---");
        indexContent.AppendLine("");
        indexContent.AppendLine("# ChangeLog");
        indexContent.AppendLine("");
        foreach (var version in DocumentationHelper.BdnAllVersions.Reverse())
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
        foreach (var version in DocumentationHelper.BdnAllVersions.Reverse())
            fullContent.AppendLine($"[!include[{version}]({version}.md)]");
        context.FileWriteText(context.ChangeLogDirectory.CombineWithFilePath("full.md"), fullContent.ToString());

        context.Information("DocfxChangelogGenerate: toc.yml");
        var tocContent = new StringBuilder();
        foreach (var version in DocumentationHelper.BdnAllVersions.Reverse())
        {
            tocContent.AppendLine($"- name: {version}");
            tocContent.AppendLine($"  href: {version}.md");
        }

        tocContent.AppendLine("- name: Full ChangeLog");
        tocContent.AppendLine("  href: full.md");
        context.FileWriteText(context.ChangeLogDirectory.CombineWithFilePath("toc.yml"), tocContent.ToString());
    }
}

[TaskName("DocFX_Generate_Redirects")]
public class DocfxGenerateRedirectsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.GenerateRedirects();
    }
}

// In order to work around xref issues in DocFx, BenchmarkDotNet and BenchmarkDotNet.Annotations must be build
// before running the DocFX_Build target. However, including a dependency on BuildTask here may have unwanted
// side effects (CleanTask).
// TODO: Define dependencies when a CI workflow scenario for using the "DocFX_Build" target exists.
[TaskName("DocFX_Build")]
[IsDependentOn(typeof(DocfxChangelogGenerateTask))]
public class DocfxBuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.RunDocfx(context.DocfxJsonFile);
        context.GenerateRedirects();
    }
}
