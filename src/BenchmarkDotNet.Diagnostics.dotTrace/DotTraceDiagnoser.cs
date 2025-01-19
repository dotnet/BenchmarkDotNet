using System;
using System.Reflection;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using JetBrains.Profiler.SelfApi;

namespace BenchmarkDotNet.Diagnostics.dotTrace;

public class DotTraceDiagnoser(Uri? nugetUrl = null, string? downloadTo = null) : SnapshotProfilerBase
{
    public override string ShortName => "dotTrace";

    protected override void InitTool(Progress progress)
    {
        DotTrace.InitAsync(progress, nugetUrl, NuGetApi.V3, downloadTo).Wait();
    }

    protected override void AttachToCurrentProcess(string snapshotFile)
    {
        DotTrace.Attach(new DotTrace.Config().SaveToFile(snapshotFile));
        DotTrace.StartCollectingData();
    }

    protected override void AttachToProcessByPid(int pid, string snapshotFile)
    {
        DotTrace.Attach(new DotTrace.Config().ProfileExternalProcess(pid).SaveToFile(snapshotFile));
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

    internal override bool IsSupported(RuntimeMoniker runtimeMoniker)
    {
        switch (runtimeMoniker)
        {
            case RuntimeMoniker.HostProcess:
            case RuntimeMoniker.Net461:
            case RuntimeMoniker.Net462:
            case RuntimeMoniker.Net47:
            case RuntimeMoniker.Net471:
            case RuntimeMoniker.Net472:
            case RuntimeMoniker.Net48:
            case RuntimeMoniker.Net481:
            case RuntimeMoniker.Net50:
            case RuntimeMoniker.Net60:
            case RuntimeMoniker.Net70:
            case RuntimeMoniker.Net80:
            case RuntimeMoniker.Net90:
            case RuntimeMoniker.Net10_0:
                return true;
            case RuntimeMoniker.NotRecognized:
            case RuntimeMoniker.Mono:
            case RuntimeMoniker.NativeAot60:
            case RuntimeMoniker.NativeAot70:
            case RuntimeMoniker.NativeAot80:
            case RuntimeMoniker.NativeAot90:
            case RuntimeMoniker.NativeAot10_0:
            case RuntimeMoniker.Wasm:
            case RuntimeMoniker.WasmNet50:
            case RuntimeMoniker.WasmNet60:
            case RuntimeMoniker.WasmNet70:
            case RuntimeMoniker.WasmNet80:
            case RuntimeMoniker.WasmNet90:
            case RuntimeMoniker.WasmNet10_0:
            case RuntimeMoniker.MonoAOTLLVM:
            case RuntimeMoniker.MonoAOTLLVMNet60:
            case RuntimeMoniker.MonoAOTLLVMNet70:
            case RuntimeMoniker.MonoAOTLLVMNet80:
            case RuntimeMoniker.MonoAOTLLVMNet90:
            case RuntimeMoniker.MonoAOTLLVMNet10_0:
            case RuntimeMoniker.Mono60:
            case RuntimeMoniker.Mono70:
            case RuntimeMoniker.Mono80:
            case RuntimeMoniker.Mono90:
            case RuntimeMoniker.Mono10_0:
#pragma warning disable CS0618 // Type or member is obsolete
            case RuntimeMoniker.NetCoreApp50:
#pragma warning restore CS0618 // Type or member is obsolete
                return false;
            case RuntimeMoniker.NetCoreApp31:
                return OsDetector.IsWindows() || OsDetector.IsLinux();
            default:
                throw new ArgumentOutOfRangeException(nameof(runtimeMoniker), runtimeMoniker, $"Runtime moniker {runtimeMoniker} is not supported");
        }
    }
}
