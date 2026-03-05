using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class WakeLockTests : BenchmarkTestExecutor
{
    private const string PingEventName = @"Global\WakeLockTests-ping";
    private const string PongEventName = @"Global\WakeLockTests-pong";
    private static readonly TimeSpan testTimeout = TimeSpan.FromMinutes(1);
    private readonly OutputLogger logger;

    public WakeLockTests(ITestOutputHelper output) : base(output)
    {
        logger = new OutputLogger(Output);
    }

    [Fact]
    public void ConfigurationDefaultValue()
    {
        Assert.Equal(WakeLockType.System, DefaultConfig.Instance.WakeLock);
        Assert.Equal(WakeLockType.None, new DebugBuildConfig().WakeLock);
        Assert.Equal(WakeLockType.None, new DebugInProcessConfig().WakeLock);
    }

    [TheoryEnvSpecific(EnvRequirement.NonWindows)]
    [InlineData(WakeLockType.None)]
    [InlineData(WakeLockType.System)]
    [InlineData(WakeLockType.Display)]
    public void WakeLockIsWindowsOnly(WakeLockType wakeLockType)
    {
        using var wakeLock = WakeLock.Request(wakeLockType, "dummy", logger);
        Assert.Null(wakeLock);
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void WakeLockSleepOrDisplayIsAllowed()
    {
        using var wakeLock = WakeLock.Request(WakeLockType.None, "dummy", logger);
        Assert.Null(wakeLock);
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly, EnvRequirement.NeedsPrivilegedProcess)]
    public void WakeLockRequireSystem()
    {
        using (var wakeLock = WakeLock.Request(WakeLockType.System, "WakeLockTests", logger))
        {
            Assert.NotNull(wakeLock);
            Assert.Equal("SYSTEM", GetPowerRequests("WakeLockTests"));
        }
        Assert.Equal("", GetPowerRequests());
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly, EnvRequirement.NeedsPrivilegedProcess)]
    public void WakeLockRequireDisplay()
    {
        using (var wakeLock = WakeLock.Request(WakeLockType.Display, "WakeLockTests", logger))
        {
            Assert.NotNull(wakeLock);
            Assert.Equal("DISPLAY, SYSTEM", GetPowerRequests("WakeLockTests"));
        }
        Assert.Equal("", GetPowerRequests());
    }

    [FactEnvSpecific(EnvRequirement.NonWindows)]
    public void BenchmarkRunnerIgnoresWakeLock() =>
        _ = CanExecute<IgnoreWakeLock>(fullValidation: false);

    [WakeLock(WakeLockType.Display)]
    public class IgnoreWakeLock
    {
        [Benchmark] public void Sleep() { }
    }

    [SupportedOSPlatform("windows")]
    [TheoryEnvSpecific(EnvRequirement.WindowsOnly, EnvRequirement.NeedsPrivilegedProcess)]
    [InlineData(typeof(Default), "SYSTEM")]
    [InlineData(typeof(None), "")]
    [InlineData(typeof(RequireSystem), "SYSTEM")]
    [InlineData(typeof(RequireDisplay), "DISPLAY, SYSTEM")]
    public async Task BenchmarkRunnerAcquiresWakeLock(Type type, string expected)
    {
        using EventWaitHandle
            ping = new EventWaitHandle(false, EventResetMode.AutoReset, PingEventName),
            pong = new EventWaitHandle(false, EventResetMode.AutoReset, PongEventName);
        string? pwrRequests = null;
        Task task = WaitForBenchmarkRunningAndGetPowerRequests();
        _ = CanExecute(type, fullValidation: false);
        await task;

        Assert.Equal(expected, pwrRequests);

        async Task WaitForBenchmarkRunningAndGetPowerRequests()
        {
            await AsTask(ping, testTimeout);
            pwrRequests = GetPowerRequests("BenchmarkDotNet Running Benchmarks");
            pong.Set();
        }
    }

    public class Default : Base { }

    [WakeLock(WakeLockType.None)] public class None : Base { }

    [WakeLock(WakeLockType.System)] public class RequireSystem : Base { }

    [WakeLock(WakeLockType.Display)] public class RequireDisplay : Base { }

    public class Base
    {
        [Benchmark]
        [SupportedOSPlatform("windows")]
        public void SignalBenchmarkRunningAndWaitForGetPowerRequests()
        {
            using EventWaitHandle
                ping = EventWaitHandle.OpenExisting(PingEventName),
                pong = EventWaitHandle.OpenExisting(PongEventName);
            ping.Set();
            pong.WaitOne(testTimeout);
        }
    }

    private string GetPowerRequests(string? expectedReason = null)
    {
        string pwrRequests = ProcessHelper.RunAndReadOutput("powercfg", "/requests")!;
        Output.WriteLine(pwrRequests); // Useful to analyse failing tests.
        string fileName = Process.GetCurrentProcess()!.MainModule!.FileName;
        string mustEndWith = fileName.Substring(Path.GetPathRoot(fileName)!.Length);

        return string.Join(", ",
            from pr in PowerRequestsParser.Parse(pwrRequests)
            where
                pr.RequesterName.EndsWith(mustEndWith, StringComparison.InvariantCulture) &&
                string.Equals(pr.RequesterType, "PROCESS", StringComparison.InvariantCulture) &&
                (expectedReason == null || string.Equals(pr.Reason, expectedReason, StringComparison.InvariantCulture))
            select pr.RequestType);
    }

    private Task AsTask(WaitHandle waitHandle, TimeSpan timeout)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        RegisteredWaitHandle? rwh = null;
        rwh = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (object? state, bool timedOut) =>
            {
                rwh?.Unregister(null);
                if (timedOut)
                {
                    tcs.SetException(new TimeoutException());
                }
                else
                {
                    tcs.SetResult(true);
                }
            },
            null,
            timeout,
            true);
        return tcs.Task;
    }
}
