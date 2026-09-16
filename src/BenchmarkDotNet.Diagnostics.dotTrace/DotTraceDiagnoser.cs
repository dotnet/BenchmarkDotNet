using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using JetBrains.Profiler.SelfApi;
using System.Reflection;

namespace BenchmarkDotNet.Diagnostics.dotTrace;

public class DotTraceDiagnoser(Uri? nugetUrl = null, string? downloadTo = null) : SnapshotProfilerBase
{
    public override string ShortName => "dotTrace";

    protected override void InitTool(Progress progress)
    {
        DotTrace.InitAsync(progress, nugetUrl, NuGetApi.V3, downloadTo).GetAwaiter().GetResult();
    }

    protected override void AttachToCurrentProcess(string snapshotFile)
    {
        DotTrace.Attach(new DotTrace.Config().SaveToFile(snapshotFile));
        DotTrace.StartCollectingData();
    }

    protected override void AttachToProcessByPid(int pid, string snapshotFile)
    {
        var config = new DotTrace.Config()
            .UseCustomResponseTimeout(milliseconds: 60 * 1000)
            .ProfileExternalProcess(pid)
            .SaveToFile(snapshotFile);
        DotTrace.Attach(config);
        DotTrace.StartCollectingData();
    }

    protected override void TakeSnapshot()
    {
        DotTrace.StopCollectingData();
        DotTrace.SaveData();
    }

    protected override void Detach()
    {
        DotTrace.Detach();
    }

    protected override string CreateSnapshotFilePath(DiagnoserActionParameters parameters)
    {
        return ArtifactFileNameHelper.GetFilePath(parameters, "snapshots", DateTime.Now, "dtp", ".0000".Length);
    }

    protected override string GetRunnerPath()
    {
        var consoleRunnerPackageField = typeof(DotTrace).GetField("ConsoleRunnerPackage", BindingFlags.NonPublic | BindingFlags.Static);
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
