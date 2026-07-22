using BenchmarkDotNet.Build.Helpers;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Run;
using Cake.Core;
using Cake.Core.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Build.Runners;

public class UnitTestRunner(BuildContext context)
{
    private FilePath UnitTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.Tests")
        .CombineWithFilePath("BenchmarkDotNet.Tests.csproj");

    private FilePath ExporterTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.Exporters.Plotting.Tests")
        .CombineWithFilePath("BenchmarkDotNet.Exporters.Plotting.Tests.csproj");

    private FilePath AnalyzerTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.Analyzers.Tests")
        .CombineWithFilePath("BenchmarkDotNet.Analyzers.Tests.csproj");

    private FilePath IntegrationTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.IntegrationTests")
        .CombineWithFilePath("BenchmarkDotNet.IntegrationTests.csproj");

    private DirectoryPath TestOutputDirectory { get; } = context.RootDirectory
        .Combine("TestResults");

    private DotNetRunSettings GetTestSettingsParameters(FilePath logFile, string tfm)
    {
        // Enabled `Trace` level logging when debug logging is enabled on GitHub Actions.
        // https://docs.github.com/en/actions/how-tos/monitor-workflows/enable-debug-logging
        var diagnosticVerbosity = context.Environment.GetEnvironmentVariable("ACTIONS_STEP_DEBUG") == "true"
             ? "Trace"
             : "Warning"; // Logging Warning/Error/Critical level logs by default.

        var settings = new DotNetRunSettings
        {
            Configuration = context.BuildConfiguration,
            Framework = tfm,
            NoBuild = true,
            NoRestore = true,
            EnvironmentVariables =
            {
                ["Platform"] = "" // force the tool to not look for the .dll in platform-specific directory
            },
            ArgumentCustomization = args
                => args.Append("--report-xunit-trx")
                       .AppendSwitchQuoted("--report-xunit-trx-filename", System.IO.Path.GetFileName(logFile.FullPath))
                       .Append("--no-ansi")
                       .AppendSwitch("--progress", "off")
                       .AppendSwitch("--output", "Detailed")
                       .AppendSwitch("--show-stdout", "Failed")
                       .Append("--diagnostic")
                       .AppendSwitch("--diagnostic-verbosity", diagnosticVerbosity)
                       .Append("--no-launch-profile"),
        };
        return settings;
    }

    private void RunTests(FilePath projectFile, string alias, string tfm)
    {
        var os = Utils.GetOs();
        var arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
        var trxFileName = $"{os}({arch})-{alias}-{tfm}.trx";
        var trxFile = TestOutputDirectory.CombineWithFilePath(trxFileName);
        var settings = GetTestSettingsParameters(trxFile, tfm);

        context.Information($"Run tests for {projectFile} ({tfm}), result file: '{trxFile}'");
        context.DotNetRun(projectFile.FullPath, settings);
    }

    private void RunUnitTests(string tfm)
    {
        RunTests(UnitTestsProjectFile, "unit", tfm);
        RunTests(ExporterTestsProjectFile, "exporters", tfm);
    }

    public void RunUnitTests()
    {
        string[] targetFrameworks = Utils.GetTargetFrameworks(context);
        foreach (var targetFramework in targetFrameworks)
            RunUnitTests(targetFramework);
    }

    public void RunAnalyzerTests()
    {
        string[] targetFrameworks = Utils.GetTargetFrameworks(context);
        foreach (var targetFramework in targetFrameworks)
            RunTests(AnalyzerTestsProjectFile, "analyzer", targetFramework);
    }

    public void RunInTests(string tfm) => RunTests(IntegrationTestsProjectFile, "integration", tfm);
}