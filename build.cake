// ARGUMENTS
var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");
var skipTests = Argument("SkipTests", false);

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts");
var solutionFile = "./BenchmarkDotNet.sln";
var solutionFileBackup = solutionFile + ".build.backup";
var isRunningOnWindows = IsRunningOnWindows();
var IsOnAppVeyorAndNotPR = AppVeyor.IsRunningOnAppVeyor && !AppVeyor.Environment.PullRequest.IsPullRequest;

Setup(_ =>
{
    StartProcess("dotnet", new ProcessSettings { Arguments = "--info" });
    if(!isRunningOnWindows)
    {
        StartProcess("mono", new ProcessSettings { Arguments = "--version" });
        
        // create solution backup
        CopyFile(solutionFile, solutionFileBackup);
        // and exclude VB projects
        ExcludeVBProjectsFromSolution();
    }
});

Teardown(_ =>
{
    if(!isRunningOnWindows && BuildSystem.IsLocalBuild)
    {
        if(FileExists(solutionFileBackup)) // Revert back solution file
        {
            DeleteFile(solutionFile);
            MoveFile(solutionFileBackup, solutionFile);
        } 
    }
});

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);

        if(BuildSystem.IsLocalBuild)
        {
            CleanDirectories(GetDirectories("./**/obj") + GetDirectories("./**/bin"));
        }
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetCoreRestore(solutionFile);
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var buildSettings = new MSBuildSettings()
            .SetConfiguration(configuration)
            .WithTarget("Rebuild")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.Default)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetPlatformTarget(PlatformTarget.MSIL) // Any CPU
            .SetMaxCpuCount(0) // parallel
            .SetNodeReuse(true);

        if(!isRunningOnWindows)
        {
            // hack for Linux bug - missing MSBuild path
            if(new CakePlatform().Family == PlatformFamily.Linux)
            {
                var monoVersion = GetMonoVersionMoniker();
                var isMSBuildSupported = monoVersion != null && System.Text.RegularExpressions.Regex.IsMatch(monoVersion,@"([5-9]|\d{2,})\.\d+\.\d+(\.\d+)?");
                if(isMSBuildSupported)
                {
                    buildSettings.ToolPath = new FilePath(@"/usr/lib/mono/msbuild/15.0/bin/MSBuild.dll");
                    Information(string.Format("Mono supports MSBuild. Override ToolPath value with `{0}`", buildSettings.ToolPath));
                }
            }
        }

        var path = MakeAbsolute(new DirectoryPath(solutionFile));
        MSBuild(path.FullPath, buildSettings);
    });

Task("FastTests")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
    {
        DotNetCoreTest("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", GetTestSettings());
    });

Task("SlowTests")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
    {
        DotNetCoreTest("./tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj", GetTestSettings());
    });

Task("Pack")
    .IsDependentOn("Build")
    .WithCriteria(IsOnAppVeyorAndNotPR || string.Equals(target, "pack", StringComparison.OrdinalIgnoreCase))
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDirectory,
            NoBuild = true
        };

        var projects = GetFiles("./src/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("Default")
    .IsDependentOn("FastTests")
    .IsDependentOn("SlowTests")
    .IsDependentOn("Pack");

RunTarget(target);

// HELPERS
private DotNetCoreTestSettings GetTestSettings()
{
    var settings = new DotNetCoreTestSettings
    {
        Configuration = "Release",
        NoBuild = true
    };

    if (!IsRunningOnWindows())
    {
        settings.Framework = "netcoreapp1.1";
    }

    return settings;
}

private void ExcludeVBProjectsFromSolution()
{
    // exclude projects
    var vbProjects = GetFiles("./tests/**/*.vbproj");
    var projects = string.Join(" ", vbProjects.Select(x => string.Format("\"{0}\"", x))); // if path contains spaces
    StartProcess("dotnet", new ProcessSettings { Arguments = "sln remove " + projects });
}

private string GetMonoVersionMoniker()
{
    var type = Type.GetType("Mono.Runtime");
    if (type != null)
    {
        var displayName = type.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (displayName != null)
        {
            return displayName.Invoke(null, null).ToString();
        }
    }
    return null;
}