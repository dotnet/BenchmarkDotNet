﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class EtwProfilerConfig
    {
        public const ulong MatchAnyKeywords = ulong.MaxValue;
        
        public bool PerformExtraBenchmarksRun { get; }
        
        public int BufferSizeInMb { get; }
        
        public float CpuSampleIntervalInMiliseconds { get; }

        public KernelTraceEventParser.Keywords KernelKeywords { get; }

        public IReadOnlyDictionary<HardwareCounter, Func<ProfileSourceInfo, int>> IntervalSelectors { get; }
        
        public IReadOnlyCollection<(Guid providerGuid, TraceEventLevel providerLevel, ulong keywords, TraceEventProviderOptions options)> Providers { get; }

        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. True by default.</param>
        /// <param name="bufferSizeInMb">ETW session buffer size, in MB. 256 by default</param>
        /// <param name="cpuSampleIntervalInMiliseconds">The rate at which CPU samples are collected. By default this is 1 (once a millisecond per CPU). There is a lower bound on this (typically 0.125 ms)</param>
        /// <param name="intervalSelectors">interval per harwdare counter, if not provided then default values will be used.</param>
        /// <param name="kernelKeywords">kernel session keywords, ImageLoad (for native stack frames) and Profile (for CPU Stacks) are the defaults</param>
        /// <param name="providers">providers that should be enabled, if not provided then default values will be used</param>
        public EtwProfilerConfig(
            bool performExtraBenchmarksRun = true, 
            int bufferSizeInMb = 256,
            float cpuSampleIntervalInMiliseconds = 1.0f,
            KernelTraceEventParser.Keywords kernelKeywords = KernelTraceEventParser.Keywords.ImageLoad | KernelTraceEventParser.Keywords.Profile,
            IReadOnlyDictionary<HardwareCounter, Func<ProfileSourceInfo, int>> intervalSelectors = null,
            IReadOnlyCollection<(Guid providerGuid, TraceEventLevel providerLevel, ulong keywords, TraceEventProviderOptions options)> providers = null)
        {
            KernelKeywords = kernelKeywords;
            PerformExtraBenchmarksRun = performExtraBenchmarksRun;
            BufferSizeInMb = bufferSizeInMb;
            CpuSampleIntervalInMiliseconds = cpuSampleIntervalInMiliseconds;
            IntervalSelectors = intervalSelectors ?? new Dictionary<HardwareCounter, Func<ProfileSourceInfo, int>>
            {
                // following values come from xunit-performance, were selected based on a many trace files from benchmark runs
                // to keep good balance between accuracy and trace file size
                { HardwareCounter.InstructionRetired, _ => 1_000_000 },
                { HardwareCounter.BranchMispredictions, _ => 1_000 },
                { HardwareCounter.CacheMisses, _ => 1_000 }
            };
            Providers = providers ?? new (Guid providerGuid, TraceEventLevel providerLevel, ulong keywords, TraceEventProviderOptions options)[]
            {
                // following values come from xunit-performance, were selected by the .NET Runtime Team
                (ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                    (ulong) (ClrTraceEventParser.Keywords.Exception
                             | ClrTraceEventParser.Keywords.GC
                             | ClrTraceEventParser.Keywords.Jit
                             | ClrTraceEventParser.Keywords.JitTracing // for the inlining events
                             | ClrTraceEventParser.Keywords.Loader
                             | ClrTraceEventParser.Keywords.NGen),
                    new TraceEventProviderOptions { StacksEnabled = false }) // stacks are too expensive for our purposes
            };
        }
    }
}