// ARGUMENTS
var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");
var skipTests = Argument("SkipTests", false);

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts");
var solutionFile = "./BenchmarkDotNet.sln";
var solutionFileBackup = solutionFile + ".build.backup";
var integrationTestsProjectPath = "./tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj";
var isRunningOnWindows = IsRunningOnWindows();
var IsOnAppVeyorAndNotPR = AppVeyor.IsRunningOnAppVeyor && !AppVeyor.Environment.PullRequest.IsPullRequest;

Setup(_ =>
{
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
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetPlatformTarget(PlatformTarget.MSIL) // Any CPU
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
                    Information(string.Format("Auto-detected ToolPath value is `{0}`", buildSettings.ToolPath));
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
        DotNetCoreTool("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", "xunit", GetTestSettingsParameters());
    });

Task("BackwardCompatibilityTests")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
    {
        var testSettings = GetTestSettingsParameters("netcoreapp1.1");
        testSettings += " -trait \"Category=BackwardCompatibility\"";

        DotNetCoreTool(integrationTestsProjectPath, "xunit", testSettings);
    });
    
Task("SlowTestsNet46")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests && isRunningOnWindows)
    .Does(() =>
    {
        DotNetCoreTool(integrationTestsProjectPath, "xunit", GetTestSettingsParameters("net46"));
    });    
    
Task("SlowTestsNetCore2")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
    {
        DotNetCoreTool(integrationTestsProjectPath, "xunit", GetTestSettingsParameters("netcoreapp2.0"));
    });       

Task("Pack")
    .IsDependentOn("Build")
    .WithCriteria((IsOnAppVeyorAndNotPR || string.Equals(target, "pack", StringComparison.OrdinalIgnoreCase)) && isRunningOnWindows)
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDirectory
        };

        var projects = GetFiles("./src/**/*.csproj");
        foreach(var project in projects.Where(p => !p.FullPath.Contains("Disassembler")))
        {
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("FastTests")
    .IsDependentOn("SlowTestsNet46")
    .IsDependentOn("SlowTestsNetCore2")
    .IsDependentOn("BackwardCompatibilityTests")
    .IsDependentOn("Pack");

RunTarget(target);

// HELPERS
private string GetTestSettingsParameters(string tfm = null)
{
    var settings = "-configuration Release -stoponfail -maxthreads unlimited -nobuild";

    if (!IsRunningOnWindows())
    {
        settings += " -framework netcoreapp2.0";
    }
    else if(!string.IsNullOrEmpty(tfm))
    {
        settings += $" -framework {tfm}";
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