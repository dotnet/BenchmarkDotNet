using System.Buffers;
using System.Diagnostics.Tracing;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(CustomConfig))]
    public class IntroEventPipeProfilerAdvanced
    {
        private class CustomConfig : ManualConfig
        {
            public CustomConfig()
            {
                AddJob(Job.ShortRun.WithRuntime(CoreRuntime.Core50));

                var providers = new[]
                {
                    new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose,
                        (long) (ClrTraceEventParser.Keywords.Exception
                        | ClrTraceEventParser.Keywords.GC
                        | ClrTraceEventParser.Keywords.Jit
                        | ClrTraceEventParser.Keywords.JitTracing // for the inlining events
                        | ClrTraceEventParser.Keywords.Loader
                        | ClrTraceEventParser.Keywords.NGen)),
                    new EventPipeProvider("System.Buffers.ArrayPoolEventSource", EventLevel.Informational, long.MaxValue),
                };

                AddDiagnoser(new EventPipeProfiler(providers: providers));
            }
        }

        [Benchmark]
        public void RentAndReturn_Shared()
        {
            var pool = ArrayPool<byte>.Shared;
            byte[] array = pool.Rent(10000);
            pool.Return(array);
        }
    }
}