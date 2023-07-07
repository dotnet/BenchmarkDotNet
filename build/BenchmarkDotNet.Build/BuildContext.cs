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

    public void GenerateFile(FilePath filePath, StringBuilder content)
    {
        GenerateFile(filePath, content.ToString());
    }

    public void GenerateFile(FilePath filePath, string content)
    {
        var relativePath = RootDirectory.GetRelativePath(filePath);
        if (this.FileExists(filePath))
        {
            var oldContent = this.FileReadText(filePath);
            if (content == oldContent)
                return;

            this.FileWriteText(filePath, content);
            this.Information("[Updated] " + relativePath);
        }
        else
        {
            this.FileWriteText(filePath, content);
            this.Information("[Generated] " + relativePath);
        }
    }

    public void Clone(DirectoryPath path, string repoUrl, string branchName)
    {
        this.Information($"[GitClone]");
        this.Information($"  Repo: {repoUrl}");
        this.Information($"  Branch: {branchName}");
        this.Information($"  Path: {path}");
        var settings = new GitCloneSettings { Checkout = true, BranchName = branchName };
        try
        {
            this.GitClone(repoUrl, path, settings);
            this.Information("  Success");
        }
        catch (Exception e)
        {
            this.Error($"  Failed to clone via API (Exception: {e.GetType().Name})'");
            try
            {
                var gitArgs = $"clone -b {branchName} {repoUrl} {path}";
                this.Information($"  Trying to clone manually using 'git {gitArgs}'");
                this.StartProcess("git", gitArgs);
                this.Information("  Success");
            }
            catch (Exception e2)
            {
                throw new Exception($"Failed to clone {repoUrl} to {path} (branch: '{branchName})'", e2);
            }
        }
    }
}