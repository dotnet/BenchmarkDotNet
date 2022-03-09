using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Diagnostics.Tracing;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class EventPipeProfilerTests
    {
        private static readonly EventPipeProvider SampleProfiler = new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational);
        private static readonly EventPipeProvider Mandatory = new EventPipeProvider(EngineEventSource.SourceName, EventLevel.Informational, long.MaxValue);

        [Theory]
        [InlineData(EventPipeProfile.CpuSampling)]
        [InlineData(EventPipeProfile.GcCollect)]
        [InlineData(EventPipeProfile.GcVerbose)]
        [InlineData(EventPipeProfile.Jit)]
        public void MandatorySettingsAreAlwaysEnabled(EventPipeProfile eventPipeProfile)
        {
            var result = EventPipeProfiler.MapToProviders(eventPipeProfile, null);

            Assert.Contains(Mandatory, result);
        }

        [Fact]
        public void UserSettingsOverrideDefaultSettings()
        {
            const string DotNetRuntime = "Microsoft-Windows-DotNETRuntime";

            var defaultSettings = new EventPipeProvider(
                DotNetRuntime,
                EventLevel.Informational,
                (long)ClrTraceEventParser.Keywords.Default);

            var userSettings = new EventPipeProvider(
                DotNetRuntime,
                EventLevel.Verbose,
                (long) (ClrTraceEventParser.Keywords.Default | ClrTraceEventParser.Keywords.JitTracing));

            var result = EventPipeProfiler.MapToProviders(EventPipeProfile.CpuSampling, new[] { userSettings });

            var final = result.Single(x => x.Name == userSettings.Name);
            Assert.Equal(userSettings.EventLevel, final.EventLevel);
            Assert.Equal(userSettings.Keywords, final.Keywords);

            Assert.Contains(SampleProfiler, result);
        }
    }
}
