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

DotNetCoreMSBuildSettings msBuildSettings = new DotNetCoreMSBuildSettings();

Setup(_ =>
{
    if(!isRunningOnWindows)
    {
        StartProcess("mono", new ProcessSettings { Arguments = "--version" });
        var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath;
        // setup correct version
        frameworkPathOverride = System.IO.Path.Combine(System.IO.Directory.GetParent(frameworkPathOverride).FullName, "4.6-api/");
        Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
        msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
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
        var path = MakeAbsolute(new DirectoryPath(solutionFile));
        DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings()
        {
            Configuration = configuration,
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = msBuildSettings,
            Verbosity = DotNetCoreVerbosity.Minimal
        });
    });

Task("FastTests")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests)
    .Does(() =>
    {
        string[] targetVersions = IsRunningOnWindows() ? 
                new []{"net46", "netcoreapp1.1", "netcoreapp2.0"}
                :
                new []{"netcoreapp2.0"};

        foreach(var version in targetVersions)
        {
            DotNetCoreTool("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", "xunit", GetTestSettingsParameters(version));
        }
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
private string GetTestSettingsParameters(string tfm)
{
    var settings = $"-configuration Release -stoponfail -maxthreads unlimited -nobuild  -framework {tfm}";
    if(string.Equals("netcoreapp2.0", tfm, StringComparison.OrdinalIgnoreCase))
    {
        settings += " --fx-version 2.0.5";
    }
    if(string.Equals("netcoreapp1.1", tfm, StringComparison.OrdinalIgnoreCase))
    {
        settings += " --fx-version 1.1.6";
    }
    
    return settings;
}
