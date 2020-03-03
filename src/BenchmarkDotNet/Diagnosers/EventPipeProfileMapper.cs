using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnosers
{
    internal sealed class EventPipeProfileMapper
    {
        private const string DotNetRuntimeProviderName = "Microsoft-Windows-DotNETRuntime";

        internal static IReadOnlyDictionary<EventPipeProfile, IReadOnlyList<EventPipeProvider>> DotNetRuntimeProfiles { get; } = new Dictionary<EventPipeProfile, IReadOnlyList<EventPipeProvider>>
        {
            { EventPipeProfile.CpuSampling,
                new[]
                {
                    new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
                    new EventPipeProvider(DotNetRuntimeProviderName, EventLevel.Informational, (long) ClrTraceEventParser.Keywords.Default)
                }},
            { EventPipeProfile.GcVerbose,
                new[]
                {
                    new EventPipeProvider(
                        name: DotNetRuntimeProviderName,
                        eventLevel: EventLevel.Verbose,
                        keywords: (long) ClrTraceEventParser.Keywords.GC |
                                  (long) ClrTraceEventParser.Keywords.GCHandle |
                                  (long) ClrTraceEventParser.Keywords.Exception
                    ),
                }},
            { EventPipeProfile.GcCollect,
                new[]
                {
                    new EventPipeProvider(
                        name: DotNetRuntimeProviderName,
                        eventLevel: EventLevel.Informational,
                        keywords: (long) ClrTraceEventParser.Keywords.GC |
                                  (long) ClrTraceEventParser.Keywords.Exception
                    )
                }},
            { EventPipeProfile.Jit,
                new[]
                {
                    new EventPipeProvider(
                        name: DotNetRuntimeProviderName,
                        eventLevel: EventLevel.Verbose,
                        keywords: (long) ClrTraceEventParser.Keywords.Jit |
                                  (long) ClrTraceEventParser.Keywords.JitTracing |
                                  (long) ClrTraceEventParser.Keywords.Exception
                    )
                }},
        };
    }
}
