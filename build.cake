// ARGUMENTS
var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");
var skipTests = Argument("SkipTests", false);

// GLOBAL VARIABLES
var artifactsDirectory = Directory("./artifacts");
var solutionFile = "./BenchmarkDotNet.sln";
var integrationTestsProjectPath = "./tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj";
var isRunningOnWindows = IsRunningOnWindows();
var IsOnAppVeyorAndNotPR = AppVeyor.IsRunningOnAppVeyor && !AppVeyor.Environment.PullRequest.IsPullRequest;

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    MaxCpuCount = 1
};

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
    else
    {
        msBuildSettings.WithProperty("UseSharedCompilation", "false");
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
        DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings
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
                new []{"net46", "netcoreapp2.1"}
                :
                new []{"netcoreapp2.1"};

        foreach(var version in targetVersions)
        {
            DotNetCoreTool("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", "xunit", GetTestSettingsParameters(version));
        }
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
        DotNetCoreTool(integrationTestsProjectPath, "xunit", GetTestSettingsParameters("netcoreapp2.1"));
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
    .IsDependentOn("SlowTestsNetCore2")
    .IsDependentOn("SlowTestsNet46")
    .IsDependentOn("Pack");

RunTarget(target);

// HELPERS
private string GetTestSettingsParameters(string tfm)
{
    var settings = $"-configuration {configuration} -parallel none -nobuild  -framework {tfm}";
    if(string.Equals("netcoreapp2.1", tfm, StringComparison.OrdinalIgnoreCase))
    {
        settings += " --fx-version 2.1.0-preview1-26216-03";
    }
    
    return settings;
}
