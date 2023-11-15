using BenchmarkDotNet.Build.Helpers;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core.IO;

namespace BenchmarkDotNet.Build.Runners;

public class UnitTestRunner
{
    private readonly BuildContext context;

    private FilePath UnitTestsProjectFile { get; }
    private FilePath IntegrationTestsProjectFile { get; }
    private DirectoryPath TestOutputDirectory { get; }

    public UnitTestRunner(BuildContext context)
    {
        this.context = context;
        UnitTestsProjectFile = context.RootDirectory
            .Combine("tests")
            .Combine("BenchmarkDotNet.Tests")
            .CombineWithFilePath("BenchmarkDotNet.Tests.csproj");
        IntegrationTestsProjectFile = context.RootDirectory
            .Combine("tests")
            .Combine("BenchmarkDotNet.IntegrationTests")
            .CombineWithFilePath("BenchmarkDotNet.IntegrationTests.csproj");
        TestOutputDirectory = context.RootDirectory
            .Combine("TestResults");
    }

    private DotNetTestSettings GetTestSettingsParameters(FilePath logFile, string tfm)
    {
        var settings = new DotNetTestSettings
        {
            Configuration = context.BuildConfiguration,
            Framework = tfm,
            NoBuild = true,
            NoRestore = true,
            Loggers = new[] { "trx", $"trx;LogFileName={logFile.FullPath}", "console;verbosity=detailed" },
            EnvironmentVariables =
            {
                ["Platform"] = "" // force the tool to not look for the .dll in platform-specific directory
            }
        };
        return settings;
    }

    private void RunTests(FilePath projectFile, string alias, string tfm)
    {
        var os = Utils.GetOs();
        var trxFileName = $"{os}-{alias}-{tfm}.trx";
        var trxFile = TestOutputDirectory.CombineWithFilePath(trxFileName);
        var settings = GetTestSettingsParameters(trxFile, tfm);

        context.Information($"Run tests for {projectFile} ({tfm}), result file: '{trxFile}'");
        context.DotNetTest(projectFile.FullPath, settings);
    }

    private void RunUnitTests(string tfm) => RunTests(UnitTestsProjectFile, "unit", tfm);

    public void RunUnitTests()
    {
        var targetFrameworks = context.IsRunningOnWindows()
            ? new[] { "net462", "net8.0" }
            : new[] { "net8.0" };

        foreach (var targetFramework in targetFrameworks)
            RunUnitTests(targetFramework);
    }

    public void RunInTests(string tfm) => RunTests(IntegrationTestsProjectFile, "integration", tfm);
}