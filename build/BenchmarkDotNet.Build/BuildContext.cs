using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Build.Helpers;
using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Options;
using BenchmarkDotNet.Build.Runners;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Core;
using Cake.Core.IO;
using Cake.FileHelpers;
using Cake.Frosting;

namespace BenchmarkDotNet.Build;

public class BuildContext : FrostingContext
{
    public string BuildConfiguration { get; set; } = "Release";
    public DotNetVerbosity BuildVerbosity { get; set; } = DotNetVerbosity.Minimal;

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath BuildDirectory { get; }
    public DirectoryPath ArtifactsDirectory { get; }

    public FilePath SolutionFile { get; }
    public FilePath TemplatesTestsProjectFile { get; }
    public FilePathCollection AllPackableSrcProjects { get; }
    public FilePath VersionsFile { get; }
    public FilePath CommonPropsFile { get; }
    public FilePath ReadmeFile { get; }

    public DotNetMSBuildSettings MsBuildSettingsRestore { get; }
    public DotNetMSBuildSettings MsBuildSettingsBuild { get; }
    public DotNetMSBuildSettings MsBuildSettingsPack { get; }

    private bool IsCiBuild => !this.BuildSystem().IsLocalBuild;

    public IReadOnlyCollection<string> NuGetPackageNames { get; }

    public VersionHistory VersionHistory { get; }

    public GitRunner GitRunner { get; }
    public UnitTestRunner UnitTestRunner { get; }
    public DocumentationRunner DocumentationRunner { get; }
    public BuildRunner BuildRunner { get; }
    public ReleaseRunner ReleaseRunner { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        RootDirectory = new DirectoryPath(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent?.Parent?.FullName);
        BuildDirectory = RootDirectory.Combine("build");
        ArtifactsDirectory = RootDirectory.Combine("artifacts");

        var toolFileName = context.IsRunningOnWindows() ? "dotnet.exe" : "dotnet";
        var toolFilePath = RootDirectory.Combine(".dotnet").CombineWithFilePath(toolFileName);
        context.Tools.RegisterFile(toolFilePath);

        SolutionFile = RootDirectory.CombineWithFilePath("BenchmarkDotNet.sln");

        TemplatesTestsProjectFile = RootDirectory.Combine("templates")
            .CombineWithFilePath("BenchmarkDotNet.Templates.csproj");
        AllPackableSrcProjects = new FilePathCollection(context.GetFiles(RootDirectory.FullPath + "/src/**/*.csproj")
            .Where(p => !p.FullPath.Contains("Disassembler")));

        VersionsFile = BuildDirectory.CombineWithFilePath("versions.txt");
        CommonPropsFile = BuildDirectory.CombineWithFilePath("common.props");
        ReadmeFile = RootDirectory.CombineWithFilePath("README.md");

        MsBuildSettingsRestore = new DotNetMSBuildSettings();
        MsBuildSettingsBuild = new DotNetMSBuildSettings();
        MsBuildSettingsPack = new DotNetMSBuildSettings();

        if (IsCiBuild)
        {
            System.Environment.SetEnvironmentVariable("BDN_CI_BUILD", "true");

            MsBuildSettingsBuild.MaxCpuCount = 1;
            MsBuildSettingsBuild.WithProperty("UseSharedCompilation", "false");
        }


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
                }
            }
        }

        if (KnownOptions.Stable.Resolve(this))
        {
            const string name = "NoVersionSuffix";
            const string value = "true";
            MsBuildSettingsRestore.WithProperty(name, value);
            MsBuildSettingsBuild.WithProperty(name, value);
            MsBuildSettingsPack.WithProperty(name, value);
        }

        // NativeAOT build requires VS C++ tools to be added to $path via vcvars64.bat
        // but once we do that, dotnet restore fails with:
        // "Please specify a valid solution configuration using the Configuration and Platform properties"
        if (context.IsRunningOnWindows())
        {
            MsBuildSettingsRestore.WithProperty("Platform", "Any CPU");
            MsBuildSettingsBuild.WithProperty("Platform", "Any CPU");
        }

        var nuGetPackageNames = new List<string>();
        nuGetPackageNames.AddRange(this
            .GetSubDirectories(RootDirectory.Combine("src"))
            .Select(directoryPath => directoryPath.GetDirectoryName())
            .Where(name => !name.Contains("Disassembler", StringComparison.OrdinalIgnoreCase)));
        nuGetPackageNames.Add("BenchmarkDotNet.Templates");
        nuGetPackageNames.Sort();
        NuGetPackageNames = nuGetPackageNames;

        VersionHistory = new VersionHistory(this, VersionsFile);

        GitRunner = new GitRunner(this);
        UnitTestRunner = new UnitTestRunner(this);
        DocumentationRunner = new DocumentationRunner(this);
        BuildRunner = new BuildRunner(this);
        ReleaseRunner = new ReleaseRunner(this);
    }

    public void GenerateFile(FilePath filePath, StringBuilder content)
    {
        GenerateFile(filePath, content.ToString());
    }

    public void GenerateFile(FilePath filePath, string content, bool reportNoChanges = false)
    {
        var relativePath = RootDirectory.GetRelativePath(filePath);
        if (this.FileExists(filePath))
        {
            var oldContent = this.FileReadText(filePath);
            if (content == oldContent)
            {
                if (reportNoChanges)
                    this.Information("[NoChanges] " + relativePath);
                return;
            }

            this.FileWriteText(filePath, content);
            this.Information("[Updated] " + relativePath);
        }
        else
        {
            this.FileWriteText(filePath, content);
            this.Information("[Generated] " + relativePath);
        }
    }

    public void RunOnlyInPushMode(Action action)
    {
        if (KnownOptions.Push.Resolve(this))
        {
            action();
        }
        else
            this.Information("  Skip because PushMode is disabled");
    }
}