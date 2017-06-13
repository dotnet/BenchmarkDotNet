// ARGUMENTS
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts"); 
var solutionFile = "./BenchmarkDotNet.sln";
var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild;
var isRunningOnWindows = IsRunningOnWindows();

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
        var vbProjects = GetFiles("./tests/**/*.vbproj");
        if(!isRunningOnWindows)
        {
            ExcludeVBProjectsFromSolution(vbProjects);
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
            IncludeVBProjectsToSolution(vbProjects);
        }
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
    .WithCriteria(isContinuousIntegrationBuild)
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

private void ExcludeVBProjectsFromSolution(FilePathCollection vbProjects)
{
    ProcessProjectFilesInSolution("remove", vbProjects);
}

private void IncludeVBProjectsToSolution(FilePathCollection vbProjects)
{
    ProcessProjectFilesInSolution("add", vbProjects);
}

private void ProcessProjectFilesInSolution(string action, FilePathCollection vbProjects)
{
    var projects = string.Join(" ", vbProjects.Select(x => string.Format("\"{0}\"", x))); // if path contains spaces
    StartProcess("dotnet", new ProcessSettings { Arguments = string.Format("sln {0} {1}", action, projects) });
}