// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts"); 
var solutionFile = "./BenchmarkDotNet.sln";
var solutionFileBackup = "./BenchmarkDotNet.sln.build";
var isRunningOnWindows = IsRunningOnWindows();

Setup(_ =>
{
    Information("Started running tasks.");
    StartProcess("dotnet", new ProcessSettings { Arguments = "--info" });
    if(!isRunningOnWindows)
    {
        StartProcess("mono", new ProcessSettings { Arguments = "--version" });
    }
});

Teardown(_ =>
{
    Information("Finished running tasks.");
});

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        if(!isRunningOnWindows)
        {
            // create backup
            CopyFile(solutionFile, solutionFileBackup);
            ExcludeVBProjectsFromSolution();
        }

        var path = MakeAbsolute(new DirectoryPath(solutionFile));
        MSBuild(path.FullPath, configurator =>  configurator
            .SetConfiguration(configuration)
            .WithTarget("Rebuild")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.Default)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetPlatformTarget(PlatformTarget.MSIL) // Any CPU
            .SetMaxCpuCount(0) // parallel
            .SetNodeReuse(true)
        );

        if(!isRunningOnWindows && BuildSystem.IsLocalBuild)
        {
            // Revert back solution file
            DeleteFile(solutionFile);
            MoveFile(solutionFileBackup, solutionFile);
        }
    });

Task("FastTests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreTest("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", GetTestSettings());
    });

Task("SlowTests")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor || (BuildSystem.IsLocalBuild && isRunningOnWindows))
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreTest("./tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj", GetTestSettings());
    });

Task("Pack")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .IsDependentOn("FastTests")
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
        Information("Not running on Windows - skipping tests for full .NET Framework");
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