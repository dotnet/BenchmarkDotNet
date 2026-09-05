using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using JetBrains.Profiler.SelfApi;
using System.Reflection;

namespace BenchmarkDotNet.Diagnostics.dotMemory;

public class DotMemoryDiagnoser(Uri? nugetUrl = null, string? downloadTo = null) : SnapshotProfilerBase
{
    public override string ShortName => "dotMemory";

    protected override void InitTool(Progress progress)
    {
        DotMemory.InitAsync(progress, nugetUrl, NuGetApi.V3, downloadTo).GetAwaiter().GetResult();
    }

    protected override void AttachToCurrentProcess(string snapshotFile)
    {
        DotMemory.Attach(new DotMemory.Config().SaveToFile(snapshotFile));
    }

    protected override void AttachToProcessByPid(int pid, string snapshotFile)
    {
        var config = new DotMemory.Config()
            .UseCustomResponseTimeout(milliseconds: 60 * 1000)
            .ProfileExternalProcess(pid)
            .SaveToFile(snapshotFile);
        DotMemory.Attach(config);
    }

    protected override void TakeSnapshot()
    {
        DotMemory.GetSnapshot();
    }

    protected override void Detach()
    {
        DotMemory.Detach();
    }

    protected override string CreateSnapshotFilePath(DiagnoserActionParameters parameters)
    {
        return ArtifactFileNameHelper.GetFilePath(parameters, "snapshots", DateTime.Now, "dmw", ".0000".Length);
    }

    protected override string GetRunnerPath()
    {
        var consoleRunnerPackageField = typeof(DotMemory).GetField("ConsoleRunnerPackage", BindingFlags.NonPublic | BindingFlags.Static);
        if (consoleRunnerPackageField == null)
            throw new InvalidOperationException("Field 'ConsoleRunnerPackage' not found.");

        object? consoleRunnerPackage = consoleRunnerPackageField.GetValue(null);
        if (consoleRunnerPackage == null)
            throw new InvalidOperationException("Unable to get value of 'ConsoleRunnerPackage'.");

        var consoleRunnerPackageType = consoleRunnerPackage.GetType();
        var getRunnerPathMethod = consoleRunnerPackageType.GetMethod("GetRunnerPath");
        if (getRunnerPathMethod == null)
            throw new InvalidOperationException("Method 'GetRunnerPath' not found.");

        string? runnerPath = getRunnerPathMethod.Invoke(consoleRunnerPackage, null) as string;
        if (runnerPath == null)
            throw new InvalidOperationException("Unable to invoke 'GetRunnerPath'.");

        return runnerPath;
    }

    internal override bool IsSupported(Runtime runtime) => runtime switch
    {
        ClrRuntime => true,
        R2RRuntime => true,
        CoreRuntime core when core.Version.Major < 3 => OsDetector.IsWindows(),
        CoreRuntime core when core.Version.Major < 5 => OsDetector.IsWindows() || OsDetector.IsLinux(),
        CoreRuntime => true,
        _ => false,
    };
}
