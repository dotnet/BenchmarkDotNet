#addin "Cake.FileHelpers"

// ARGUMENTS
var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");
var skipTests = Argument("SkipTests", false);

// GLOBAL VARIABLES
var toolsDirectory = "./tools/";
var docfxExe = toolsDirectory + "docfx/docfx.exe";
var docfxVersion = "2.40.10";
var changelogDir = "./docs/changelog/";
var changelogGenDir = "./docs/_changelog/";
var bdnAllVersions = new string[] {
		"v0.7.0",
		"v0.7.1",
		"v0.7.2",
		"v0.7.3",
		"v0.7.4",
		"v0.7.5",
		"v0.7.6",
		"v0.7.7",
		"v0.7.8",
		"v0.8.0",
		"v0.8.1",
		"v0.8.2",
		"v0.9.0",
		"v0.9.1",
		"v0.9.2",
		"v0.9.3",
		"v0.9.4",
		"v0.9.5",
		"v0.9.6",
		"v0.9.7",
		"v0.9.8",
		"v0.9.9",
		"v0.10.0",
		"v0.10.1",
		"v0.10.2",
		"v0.10.3",
		"v0.10.4",
		"v0.10.5",
		"v0.10.6",
		"v0.10.7",
		"v0.10.8",
		"v0.10.9",
		"v0.10.10",
		"v0.10.11",
		"v0.10.12",
		"v0.10.13",
		"v0.10.14",
		"v0.11.0",
		"v0.11.1",
		"v0.11.2",
		"v0.11.3"
	};
var bdnNextVersion = "v0.11.4";
var bdnFirstCommit = "6eda98ab1e83a0d185d09ff8b24c795711af8db1";

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
    
    msBuildSettings.WithProperty("UseSharedCompilation", "false");
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
                new []{"net461", "netcoreapp2.1"}
                :
                new []{"netcoreapp2.1"};

        foreach(var version in targetVersions)
        {
            DotNetCoreTest("./tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj", GetTestSettingsParameters(version));
        }
    });
    
Task("SlowTestsNet461")
    .IsDependentOn("Build")
    .WithCriteria(!skipTests && isRunningOnWindows)
    .Does(() =>
    {
        DotNetCoreTest(integrationTestsProjectPath, GetTestSettingsParameters("net461"));
    }); 

Task("SlowTestsNetCore2")
	.IsDependentOn("Build")
	.WithCriteria(!skipTests)
	.Does(() =>
	{
		DotNetCoreTest(integrationTestsProjectPath, GetTestSettingsParameters("netcoreapp2.1"));
	});          

Task("Pack")
    .IsDependentOn("Build")
    .WithCriteria((IsOnAppVeyorAndNotPR || string.Equals(target, "pack", StringComparison.OrdinalIgnoreCase)) && isRunningOnWindows)
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDirectory,
			ArgumentCustomization = args=>args.Append("--include-symbols").Append("-p:SymbolPackageFormat=snupkg")
        };

        var projects = GetFiles("./src/**/*.csproj");
        foreach(var project in projects.Where(p => !p.FullPath.Contains("Disassembler")))
        {
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("DocFX_Install")
	.Does(() => {
		if (!FileExists(docfxExe)) {
			DownloadFile(
				"https://github.com/dotnet/docfx/releases/download/v" + docfxVersion + "/docfx.zip",
				toolsDirectory + "docfx.zip");
			Unzip(toolsDirectory + "docfx.zip", toolsDirectory + "docfx");
		}
	});

Task("DocFX_Changelog_Download")
	.IsDependentOn("DocFX_Install")
	.Does(() => {
		DocfxChangelogDownload(bdnAllVersions.First(), bdnFirstCommit);
		for (int i = 1; i < bdnAllVersions.Length; i++)
			DocfxChangelogDownload(bdnAllVersions[i], bdnAllVersions[i - 1]);
		DocfxChangelogDownload(bdnNextVersion, bdnAllVersions.Last(), "HEAD");
	});

Task("DocFX_Changelog_Generate")
	.IsDependentOn("DocFX_Install")
	.Does(() => {
		foreach (var version in bdnAllVersions)
			DocfxChangelogGenerate(version);
		DocfxChangelogGenerate(bdnNextVersion);

		CopyFile(changelogGenDir + "index.md", changelogDir + "index.md");
		CopyFile(changelogGenDir + "full.md", changelogDir + "full.md");
	});

Task("DocFX_Build")
	.IsDependentOn("DocFX_Install")
	.IsDependentOn("DocFX_Changelog_Generate")
	.Does(() => {
		RunDocfx("docs/docfx.json");
	});

Task("DocFX_Serve")
	.IsDependentOn("DocFX_Install")
	.IsDependentOn("DocFX_Changelog_Generate")
	.Does(() => {
		RunDocfx("docs/docfx.json --serve");
	});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("FastTests")
    .IsDependentOn("SlowTestsNetCore2")
    .IsDependentOn("SlowTestsNet461")
    .IsDependentOn("Pack");

RunTarget(target);

// HELPERS
private DotNetCoreTestSettings GetTestSettingsParameters(string tfm)
{
	return new DotNetCoreTestSettings
                {
                    Configuration = configuration,
					Framework = tfm,
                    NoBuild = true,
					NoRestore = true,
					Logger = "trx"
				}; 
}

private void RunDocfx(string args)
{
	if (!isRunningOnWindows)    
        StartProcess("mono", new ProcessSettings { Arguments = docfxExe + " " + args });
	else
		StartProcess(docfxExe, new ProcessSettings { Arguments = args });
}

private void DocfxChangelogGenerate(string version)
{
	Verbose("DocfxChangelogGenerate: " + version);
	var header = changelogGenDir + "header/" + version + ".md";
	var footer = changelogGenDir + "footer/" + version + ".md";
	var details = changelogGenDir + "details/" + version + ".md";
	var release = changelogDir + version + ".md";

	var content = new StringBuilder();
	content.AppendLine("---");
	content.AppendLine("uid: changelog." + version);
	content.AppendLine("---");
	content.AppendLine("");
	content.AppendLine("# BenchmarkDotNet " + version);
	content.AppendLine("");
	content.AppendLine("");

	if (FileExists(header)) {
	    content.AppendLine(FileReadText(header));
	    content.AppendLine("");
	    content.AppendLine("");
	}

	if (FileExists(details)) {
	    content.AppendLine(FileReadText(details));
	    content.AppendLine("");
	    content.AppendLine("");
	}

	if (FileExists(footer)) {
		content.AppendLine("## Additional details");
		content.AppendLine("");
	    content.AppendLine(FileReadText(footer));
	}

	FileWriteText(release, content.ToString());
}

private void DocfxChangelogDownload(string version, string versionPrevious, string lastCommit = "")
{
	Verbose("DocfxChangelogDownload: " + version);
	// Required environment variables: GITHIB_PRODUCT, GITHUB_TOKEN
	StartProcess("dotnet", new ProcessSettings
	{ 
		WorkingDirectory = changelogGenDir + "ChangeLogBuilder",
		Arguments = "run -- " + version + " " + versionPrevious + " " + lastCommit
	});
	CopyFile(changelogGenDir + "ChangeLogBuilder/" + version + ".md", changelogGenDir + "details/" + version + ".md");
}