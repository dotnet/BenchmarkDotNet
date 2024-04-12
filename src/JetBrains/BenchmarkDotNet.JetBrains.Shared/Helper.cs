using System;
using System.Reflection;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.JetBrains.Shared
{
    internal static class Helper
    {
        internal static bool IsSupported(RuntimeMoniker runtimeMoniker)
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
                    return true;
                case RuntimeMoniker.NotRecognized:
                case RuntimeMoniker.Mono:
                case RuntimeMoniker.NativeAot60:
                case RuntimeMoniker.NativeAot70:
                case RuntimeMoniker.NativeAot80:
                case RuntimeMoniker.NativeAot90:
                case RuntimeMoniker.Wasm:
                case RuntimeMoniker.WasmNet50:
                case RuntimeMoniker.WasmNet60:
                case RuntimeMoniker.WasmNet70:
                case RuntimeMoniker.WasmNet80:
                case RuntimeMoniker.WasmNet90:
                case RuntimeMoniker.MonoAOTLLVM:
                case RuntimeMoniker.MonoAOTLLVMNet60:
                case RuntimeMoniker.MonoAOTLLVMNet70:
                case RuntimeMoniker.MonoAOTLLVMNet80:
                case RuntimeMoniker.MonoAOTLLVMNet90:
                case RuntimeMoniker.Mono60:
                case RuntimeMoniker.Mono70:
                case RuntimeMoniker.Mono80:
                case RuntimeMoniker.Mono90:
#pragma warning disable CS0618 // Type or member is obsolete
                case RuntimeMoniker.NetCoreApp50:
#pragma warning restore CS0618 // Type or member is obsolete
                    return false;
                case RuntimeMoniker.NetCoreApp20:
                case RuntimeMoniker.NetCoreApp21:
                case RuntimeMoniker.NetCoreApp22:
                    return RuntimeInformation.IsWindows();
                case RuntimeMoniker.NetCoreApp30:
                case RuntimeMoniker.NetCoreApp31:
                    return RuntimeInformation.IsWindows() || RuntimeInformation.IsLinux();
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeMoniker), runtimeMoniker, $"Runtime moniker {runtimeMoniker} is not supported");
            }
        }

        internal static string GetRunnerPath(Type type)
        {
            var consoleRunnerPackageField = type.GetField("ConsoleRunnerPackage", BindingFlags.NonPublic | BindingFlags.Static);
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
    }
}