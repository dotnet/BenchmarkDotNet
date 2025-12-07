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
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Build.Runners;

public class BuildRunner
{
    private readonly BuildContext context;

    public BuildRunner(BuildContext context)
    {
        this.context = context;
    }

    public void PackWeaver()
    {
        var weaverPath = context.AllPackableSrcProjects.Single(p => p.GetFilename() == "BenchmarkDotNet.Weaver.csproj");
        var outputPackageDir = weaverPath.GetDirectory().Combine("packages");

        // Delete old package.
        foreach (var file in Directory.EnumerateFiles(outputPackageDir.FullPath))
        {
            File.Delete(file);
        }

        context.DotNetRestore(weaverPath.GetDirectory().FullPath,
            new DotNetRestoreSettings
            {
                MSBuildSettings = context.MsBuildSettingsRestore
            });

        context.Information("BuildSystemProvider: " + context.BuildSystem().Provider);
        context.DotNetBuild(weaverPath.FullPath, new DotNetBuildSettings
        {
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Configuration = context.BuildConfiguration,
            Verbosity = context.BuildVerbosity
        });

        context.DotNetPack(weaverPath.FullPath, new DotNetPackSettings
        {
            OutputDirectory = outputPackageDir,
            MSBuildSettings = context.MsBuildSettingsPack,
            Configuration = context.BuildConfiguration
        });
    }

    public void Restore()
    {
        context.DotNetRestore(context.SolutionFile.FullPath,
            new DotNetRestoreSettings
            {
                MSBuildSettings = context.MsBuildSettingsRestore
            });
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
        context.DotNetBuild(context.SolutionFile.FullPath, new DotNetBuildSettings
        {
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Configuration = context.BuildConfiguration,
            Verbosity = context.BuildVerbosity
        });
    }

    public void BuildProjectSilent(FilePath projectFile)
    {
        context.DotNetBuild(projectFile.FullPath, new DotNetBuildSettings
        {
            NoRestore = false,
            DiagnosticOutput = false,
            MSBuildSettings = context.MsBuildSettingsBuild,
            Configuration = context.BuildConfiguration,
            Verbosity = DotNetVerbosity.Quiet
        });
    }

    public void BuildAnalyzers()
    {
        context.Information("BuildSystemProvider: " + context.BuildSystem().Provider);
        string[] mccVersions = ["2.8", "3.8", "4.8"];
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