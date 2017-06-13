// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// GLOBAL CONSTANTS
const string netcore = "netcoreapp1.1";

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts"); 
var solutionFile = "./BenchmarkDotNet.sln";
var isWindows = IsRunningOnWindows();

Setup(_ =>
{
    Information("Started running tasks.");
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
       DotNetBuild(solutionFile, settings => settings
            .SetConfiguration(configuration)
            .WithTarget("Rebuild")
            .SetVerbosity(Verbosity.Minimal));
    });

Task("FastTests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreTest("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", GetTestSettings());
    });

Task("SlowTests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreTest("./tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj", GetTestSettings());
    });

Task("Pack")
    .IsDependentOn("FastTests")
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDirectory
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
        Configuration = configuration,
        NoBuild = true
    };

    if (!isWindows)
    {
        Information("Not running on Windows - skipping tests for full .NET Framework");
        settings.Framework = netcore;
    }

    return settings;
}