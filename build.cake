// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts"); 
var solutionFile = "./BenchmarkDotNet.sln";
var isWindows = IsRunningOnWindows();

Setup(_ =>
{
    Information("Started running tasks.");
    StartProcess("dotnet", new ProcessSettings { Arguments = "--info" });
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
        MSBuild(solutionFile, configurator =>  configurator
            .SetConfiguration(configuration)
            .WithTarget("Rebuild")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.Default)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetPlatformTarget(PlatformTarget.MSIL) // Any CPU
            .SetMaxCpuCount(0) // parallel
            .SetNodeReuse(true)
        );
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
    //.IsDependentOn("SlowTests")
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