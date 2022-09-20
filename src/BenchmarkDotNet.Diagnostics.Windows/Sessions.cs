using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Extensions;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    internal class HeapSession : Session
    {
        public HeapSession(DiagnoserActionParameters details, EtwProfilerConfig config, DateTime creationTime)
            : base(GetSessionName(details.BenchmarkCase) + "Heap", details, config, creationTime)
        {
        }

        protected override string FileExtension => "userheap.etl";

        internal override Session EnableProviders()
        {
            var osHeapExe = Path.GetFileName(Path.ChangeExtension(Details.Process.StartInfo.FileName, ".exe"));
            TraceEventSession.EnableWindowsHeapProvider(osHeapExe);
            return this;
        }
    }

    internal class UserSession : Session
    {
        public UserSession(DiagnoserActionParameters details, EtwProfilerConfig config, DateTime creationTime)
            : base(GetSessionName(details.BenchmarkCase), details, config, creationTime)
        {
        }

        protected override string FileExtension => "etl";

        internal override Session EnableProviders()
        {
            TraceEventSession.EnableProvider(EngineEventSource.Log.Name, TraceEventLevel.Informational); // mandatory provider to enable Engine events

            foreach (var provider in Config.Providers)
            {
                TraceEventSession.EnableProvider(provider.providerGuid, provider.providerLevel, provider.keywords, provider.options);
            }

            return this;
        }
    }

    internal class KernelSession : Session
    {
        public KernelSession(DiagnoserActionParameters details, EtwProfilerConfig config, DateTime creationTime)
            : base(KernelTraceEventParser.KernelSessionName, details, config, creationTime)
        {
        }

        protected override string FileExtension => "kernel.etl";

        internal override Session EnableProviders()
        {
            var keywords = Config.KernelKeywords
                | KernelTraceEventParser.Keywords.ImageLoad // handles stack frames from native modules, SUPER IMPORTANT!
                | KernelTraceEventParser.Keywords.Profile; // CPU stacks

            if (Details.Config.GetHardwareCounters().Any())
                keywords |= KernelTraceEventParser.Keywords.PMCProfile; // Precise Machine Counters

            TraceEventSession.StackCompression = true;

            try
            {
                TraceEventSession.EnableKernelProvider(keywords, Config.KernelStackKeywords);
            }
            catch (Win32Exception)
            {
                Details.Config.GetCompositeLogger().WriteLineError(
                    "Please install the latest Microsoft.Diagnostics.Tracing.TraceEvent package in the project with benchmarks so MSBuild can copy the native dependencies of TraceEvent to the output folder.");

                throw;
            }

            return this;
        }
    }

    internal abstract class Session : IDisposable
    {
        private const int MaxSessionNameLength = 128;

        protected abstract string FileExtension { get; }

        protected TraceEventSession TraceEventSession { get; }

        protected DiagnoserActionParameters Details { get; }

        protected EtwProfilerConfig Config { get; }

        internal string FilePath { get; }

        protected Session(string sessionName, DiagnoserActionParameters details, EtwProfilerConfig config, DateTime creationTime)
        {
            Details = details;
            Config = config;
            FilePath = ArtifactFileNameHelper.GetTraceFilePath(details, creationTime, FileExtension).EnsureFolderExists();
            TraceEventSession = new TraceEventSession(sessionName, FilePath)
            {
                BufferSizeMB = config.BufferSizeInMb,
                CpuSampleIntervalMSec = config.CpuSampleIntervalInMilliseconds,
            };

            Console.CancelKeyPress += OnConsoleCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public void Dispose() => TraceEventSession.Dispose();

        internal void Stop()
        {
            TraceEventSession.Stop();

            Console.CancelKeyPress -= OnConsoleCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }

        internal abstract Session EnableProviders();

        private void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e) => Stop();

        private void OnProcessExit(object sender, EventArgs e) => Stop();

        protected static string GetSessionName(BenchmarkCase benchmarkCase)
        {
            string benchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase);
            if (benchmarkName.Length <= MaxSessionNameLength)
                return benchmarkName;

            // session name is not really used by humans, we can just give it the hashcode value
            return $"BenchmarkDotNet.EtwProfiler.Session_{Hashing.HashString(benchmarkName)}";
        }
    }
}