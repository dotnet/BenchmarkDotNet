using System;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Build.Helpers;
using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Runners;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Build.AppVeyor;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Core;
using Cake.Core.IO;
using Cake.FileHelpers;
using Cake.Frosting;
using Cake.Git;

namespace BenchmarkDotNet.Build;

public class BuildContext : FrostingContext
{
    public string BuildConfiguration { get; set; } = "Release";
    public DotNetVerbosity BuildVerbosity { get; set; } = DotNetVerbosity.Minimal;
    public int Depth { get; set; }

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath BuildDirectory { get; }
    public DirectoryPath ArtifactsDirectory { get; }
    public DirectoryPath DocsDirectory { get; }
    public FilePath DocfxJsonFile { get; }

    public DirectoryPath ChangeLogDirectory { get; }
    public DirectoryPath ChangeLogGenDirectory { get; }

    public DirectoryPath RedirectRootDirectory { get; }
    public DirectoryPath RedirectTargetDirectory { get; }

    public FilePath SolutionFile { get; }
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
    
    public VersionHistory VersionHistory { get; }
    
    public UnitTestRunner UnitTestRunner { get; }
    public DocumentationRunner DocumentationRunner { get; }
    public BuildRunner BuildRunner { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        RootDirectory = new DirectoryPath(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent?.Parent?.FullName);
        BuildDirectory = RootDirectory.Combine("build");
        ArtifactsDirectory = RootDirectory.Combine("artifacts");
        DocsDirectory = RootDirectory.Combine("docs");
        DocfxJsonFile = DocsDirectory.CombineWithFilePath("docfx.json");
        
        ChangeLogDirectory = RootDirectory.Combine("docs").Combine("changelog");
        ChangeLogGenDirectory = RootDirectory.Combine("docs").Combine("_changelog");

        RedirectRootDirectory = RootDirectory.Combine("docs").Combine("_redirects");
        RedirectTargetDirectory = RootDirectory.Combine("docs").Combine("_site");

        SolutionFile = RootDirectory.CombineWithFilePath("BenchmarkDotNet.sln");
        
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

        Depth = -1;
        if (context.Arguments.HasArgument("msbuild"))
        {
            var msBuildParameters = context.Arguments.GetArguments().First(it => it.Key == "msbuild").Value;
            foreach (var msBuildParameter in msBuildParameters)
            {
                var split = msBuildParameter.Split(new[] { '=' }, 2);
                if (split.Length == 2)
                {
                    var name = split[0];
                    var value = split[1];

                    MsBuildSettingsRestore.WithProperty(name, value);
                    MsBuildSettingsBuild.WithProperty(name, value);
                    MsBuildSettingsPack.WithProperty(name, value);

                    if (name.Equals("configuration", StringComparison.OrdinalIgnoreCase)) BuildConfiguration = value;

                    if (name.Equals("verbosity", StringComparison.OrdinalIgnoreCase))
                    {
                        var parsedVerbosity = Utils.ParseVerbosity(value);
                        if (parsedVerbosity != null)
                            BuildVerbosity = parsedVerbosity.Value;
                    }

                    if (name.Equals("depth", StringComparison.OrdinalIgnoreCase))
                        Depth = int.Parse(value);
                }
            }
        }
        
        // NativeAOT build requires VS C++ tools to be added to $path via vcvars64.bat
        // but once we do that, dotnet restore fails with:
        // "Please specify a valid solution configuration using the Configuration and Platform properties"
        if (context.IsRunningOnWindows())
        {
            MsBuildSettingsRestore.WithProperty("Platform", "Any CPU");
            MsBuildSettingsBuild.WithProperty("Platform", "Any CPU");
        }

        VersionHistory = new VersionHistory(this, BuildDirectory.CombineWithFilePath("versions.txt"));

        UnitTestRunner = new UnitTestRunner(this);
        DocumentationRunner = new DocumentationRunner(this);
        BuildRunner = new BuildRunner(this);
    }
    
    public void EnsureChangelogDetailsExist(bool forceClean = false)
    {
        var path = ChangeLogGenDirectory.Combine("details");
        if (this.DirectoryExists(path) && forceClean)
            this.DeleteDirectory(path, new DeleteDirectorySettings() { Force = true, Recursive = true });

        if (!this.DirectoryExists(path))
        {
            var repo = Repo.HttpsGitUrl;
            var branchName = Repo.ChangelogDetailsBranch;
            var settings = new GitCloneSettings { Checkout = true, BranchName = branchName };
            this.Information($"Trying to clone {repo} to {path} (branch: '{branchName})");
            try
            {
                this.GitClone(repo, path, settings);
            }
            catch (Exception e)
            {
                this.Error($"Failed to clone {repo} to {path} (branch: '{branchName}), Exception: {e.GetType().Name}'");
                try
                {
                    var gitArgs = $"clone -b {branchName} {repo} {path}";
                    this.Information($"Trying to clone manually: 'git {gitArgs}'");
                    this.StartProcess("git", gitArgs);
                }
                catch (Exception e2)
                {
                    throw new Exception($"Failed to clone {repo} to {path} (branch: '{branchName})'", e2);
                }
            }

            this.Information("Clone is successfully finished");
            this.Information("");
        }
    }

    public void DocfxChangelogDownload(string version, string versionPrevious, string lastCommit = "")
    {
        EnsureChangelogDetailsExist();
        this.Information("DocfxChangelogDownload: " + version);
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


}