using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.Workload.Install;
using Cake.Core;
using Cake.Core.IO;
using System;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Build.Runners;

public class BuildRunner
{
    private readonly BuildContext context;
    private readonly bool isFullPack;

    public BuildRunner(BuildContext context)
    {
        this.context = context;
        isFullPack = context.Arguments.GetArgument("target") is PackTask.Name or ReleaseTask.Name;
    }

    private void MaybeAppendArgument(DotNetSettings settings)
    {
        if (isFullPack)
        {
            settings.ArgumentCustomization = args => args.Append("-p:IsFullPack=true");
        }
    }

    public void PackWeaver()
    {
        var weaverPath = context.AllPackableSrcProjects.Single(p => p.GetFilename() == "BenchmarkDotNet.Weaver.csproj");
        var outputPackageDir = weaverPath.GetDirectory().Combine("packages");

        if (!isFullPack)
        {
            // Delete old package.
            foreach (var file in Directory.EnumerateFiles(outputPackageDir.FullPath))
            {
                File.Delete(file);
            }
        }

        var restoreSettings = new DotNetRestoreSettings
        {
            MSBuildSettings = context.MsBuildSettingsRestore,
        };
        MaybeAppendArgument(restoreSettings);
        context.DotNetRestore(weaverPath.GetDirectory().FullPath, restoreSettings);

        context.Information("BuildSystemProvider: " + context.BuildSystem().Provider);
        var buildSettings = new DotNetBuildSettings
        {
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Configuration = context.BuildConfiguration,
            Verbosity = context.BuildVerbosity
        };
        MaybeAppendArgument(buildSettings);
        context.DotNetBuild(weaverPath.FullPath, buildSettings);

        var packSettings = new DotNetPackSettings
        {
            OutputDirectory = outputPackageDir,
            MSBuildSettings = context.MsBuildSettingsPack,
            Configuration = context.BuildConfiguration
        };
        MaybeAppendArgument(packSettings);
        context.DotNetPack(weaverPath.FullPath, packSettings);
    }

    public void Restore()
    {
        var restoreSettings = new DotNetRestoreSettings
        {
            MSBuildSettings = context.MsBuildSettingsRestore,
        };
        MaybeAppendArgument(restoreSettings);
        context.DotNetRestore(context.SolutionFile.FullPath, restoreSettings);
    }

    public void InstallWorkload(string workloadId)
    {
        context.DotNetWorkloadInstall(workloadId,
            new DotNetWorkloadInstallSettings
            {
                IncludePreviews = true,
                NoCache = true
            });
    }

    public void Build()
    {
        context.Information("BuildSystemProvider: " + context.BuildSystem().Provider);
        var buildSettings = new DotNetBuildSettings
        {
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Configuration = context.BuildConfiguration,
            Verbosity = context.BuildVerbosity
        };
        MaybeAppendArgument(buildSettings);
        context.DotNetBuild(context.SolutionFile.FullPath, buildSettings);
    }

    public void BuildProjectSilent(FilePath projectFile)
    {
        var buildSettings = new DotNetBuildSettings
        {
            NoRestore = false,
            DiagnosticOutput = false,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Configuration = context.BuildConfiguration,
            Verbosity = DotNetVerbosity.Quiet
        };
        MaybeAppendArgument(buildSettings);
        context.DotNetBuild(projectFile.FullPath, buildSettings);
    }

    public void BuildAnalyzers()
    {
        context.Information("BuildSystemProvider: " + context.BuildSystem().Provider);
        string[] mccVersions = ["2.8", "3.8", "4.8", "5.0"];
        foreach (string version in mccVersions)
        {
            context.DotNetBuild(context.AnalyzersProjectFile.FullPath, new DotNetBuildSettings
            {
                NoRestore = true,
                DiagnosticOutput = true,
                MSBuildSettings = context.MsBuildSettingsBuild,
                Configuration = context.BuildConfiguration,
                Verbosity = context.BuildVerbosity,
                ArgumentCustomization = args => args.Append($"-p:MccVersion={version}")
            });
        }
    }

    public void Pack()
    {
        context.CleanDirectory(context.ArtifactsDirectory);

        var settingsSrc = new DotNetPackSettings
        {
            OutputDirectory = context.ArtifactsDirectory,
            ArgumentCustomization = args => args.Append("--include-symbols").Append("-p:SymbolPackageFormat=snupkg").Append("-p:IsFullPack=true"),
            MSBuildSettings = context.MsBuildSettingsPack,
            Configuration = context.BuildConfiguration
        };

        foreach (var project in context.AllPackableSrcProjects)
            context.DotNetPack(project.FullPath, settingsSrc);

        var settingsTemplate = new DotNetPackSettings
        {
            OutputDirectory = context.ArtifactsDirectory,
            MSBuildSettings = context.MsBuildSettingsPack,
            Configuration = context.BuildConfiguration
        };
        context.DotNetPack(context.TemplatesTestsProjectFile.FullPath, settingsTemplate);
    }
}