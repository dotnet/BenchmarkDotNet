//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=xunit.runner.console"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL CONSTANTS
///////////////////////////////////////////////////////////////////////////////
const string netcore = "netcoreapp1.1";

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
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

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
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

        var projects = GetFiles("./tests/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetCoreTest(project.FullPath, settings);
        }
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var projects = GetFiles("./src/**/*.csproj");

        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDirectory,
        };
        
        foreach(var project in projects)
	    {
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);