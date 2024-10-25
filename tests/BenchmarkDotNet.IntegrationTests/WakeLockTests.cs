using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class WakeLockTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    private const string PingEventName = @"Global\WakeLockTests-ping";
    private const string PongEventName = @"Global\WakeLockTests-pong";
    private static readonly TimeSpan testTimeout = TimeSpan.FromMinutes(1);

    [Fact]
    public void ConfigurationDefaultValue()
    {
        Assert.Equal(WakeLockType.No, DefaultConfig.Instance.WakeLock);
        Assert.Equal(WakeLockType.No, new DebugBuildConfig().WakeLock);
        Assert.Equal(WakeLockType.No, new DebugInProcessConfig().WakeLock);
    }

    [TheoryEnvSpecific(EnvRequirement.NonWindows)]
    [InlineData(WakeLockType.No)]
    [InlineData(WakeLockType.RequireSystem)]
    [InlineData(WakeLockType.RequireDisplay)]
    public void WakeLockIsWindowsOnly(WakeLockType wakeLockType)
    {
        using IDisposable wakeLock = WakeLock.Request(wakeLockType, "dummy");
        Assert.Null(wakeLock);
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void WakeLockSleepOrDisplayIsAllowed()
    {
        using IDisposable wakeLock = WakeLock.Request(WakeLockType.No, "dummy");
        Assert.Null(wakeLock);
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void WakeLockRequireSystem()
    {
        using (IDisposable wakeLock = WakeLock.Request(WakeLockType.RequireSystem, "WakeLockTests"))
        {
            Assert.NotNull(wakeLock);
            Assert.Equal("SYSTEM", GetPowerRequests("WakeLockTests"));
        }
        Assert.Equal("", GetPowerRequests());
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void WakeLockRequireDisplay()
    {
        using (IDisposable wakeLock = WakeLock.Request(WakeLockType.RequireDisplay, "WakeLockTests"))
        {
            Assert.NotNull(wakeLock);
            Assert.Equal("DISPLAY, SYSTEM", GetPowerRequests("WakeLockTests"));
        }
        Assert.Equal("", GetPowerRequests());
    }

    [FactEnvSpecific(EnvRequirement.NonWindows)]
    public void BenchmarkRunnerIgnoresWakeLock() =>
        _ = CanExecute<IgnoreWakeLock>(fullValidation: false);

    [WakeLock(WakeLockType.RequireDisplay)]
    public class IgnoreWakeLock
    {
        [Benchmark] public void Sleep() { }
    }

#if !NET462
    [SupportedOSPlatform("windows")]
#endif
    [TheoryEnvSpecific(EnvRequirement.WindowsOnly)]
    [InlineData(typeof(Sleepy), "")]
    [InlineData(typeof(RequireSystem), "SYSTEM")]
    [InlineData(typeof(RequireDisplay), "DISPLAY, SYSTEM")]
    public async Task BenchmarkRunnerAcquiresWakeLock(Type type, string expected)
    {
        using EventWaitHandle
            ping = new EventWaitHandle(false, EventResetMode.AutoReset, PingEventName),
            pong = new EventWaitHandle(false, EventResetMode.AutoReset, PongEventName);
        string pwrRequests = null;
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

    public class Sleepy : Base { }

    [WakeLock(WakeLockType.RequireSystem)] public class RequireSystem : Base { }

    [WakeLock(WakeLockType.RequireDisplay)] public class RequireDisplay : Base { }

    public class Base
    {
        [Benchmark]
#if !NET462
        [SupportedOSPlatform("windows")]
#endif
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
        Assert.True(IsAdministrator(), "'powercfg /requests' requires administrator privileges and must be executed from an elevated command prompt.");
        string pwrRequests = ProcessHelper.RunAndReadOutput("powercfg", "/requests");
        Output.WriteLine(pwrRequests); // Useful to analyse failing tests.
        string fileName = Process.GetCurrentProcess().MainModule.FileName;
        string mustEndWith = fileName.Substring(Path.GetPathRoot(fileName).Length);

        return string.Join(", ",
            from pr in PowerRequestsParser.Parse(pwrRequests)
            where
                pr.RequesterName.EndsWith(mustEndWith, StringComparison.InvariantCulture) &&
                string.Equals(pr.RequesterType, "PROCESS", StringComparison.InvariantCulture) &&
                (expectedReason == null || string.Equals(pr.Reason, expectedReason, StringComparison.InvariantCulture))
            select pr.RequestType);
    }

    private static bool IsAdministrator()
    {
#if !NET462
        // Following line prevents error CA1416: This call site is reachable on all platforms.
        // 'WindowsIdentity.GetCurrent()' is only supported on: 'windows'.
        Debug.Assert(OperatingSystem.IsWindows());
#endif
        using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(currentUser).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private Task AsTask(WaitHandle waitHandle, TimeSpan timeout)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        RegisteredWaitHandle rwh = null;
        rwh = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (object state, bool timedOut) =>
            {
                rwh.Unregister(null);
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
