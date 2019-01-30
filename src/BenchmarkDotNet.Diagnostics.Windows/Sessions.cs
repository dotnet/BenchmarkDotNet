using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    internal class UserSession : Session
    {
        public UserSession(DiagnoserActionParameters details, EtwProfilerConfig config, DateTime creationTime)
            : base(FullNameProvider.GetBenchmarkName(details.BenchmarkCase), details, config, creationTime)
        {
        }

        protected override string FileExtension => ".etl";

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
        
        protected override string FileExtension => ".kernel.etl";

        internal override Session EnableProviders()
        {
            var keywords = Config.KernelKeywords 
                | KernelTraceEventParser.Keywords.ImageLoad // handles stack frames from native modules, SUPER IMPORTANT! 
                | KernelTraceEventParser.Keywords.Profile; // CPU stacks

            if (Details.Config.GetHardwareCounters().Any())
                keywords |= KernelTraceEventParser.Keywords.PMCProfile; // Precise Machine Counters

            try
            {
                TraceEventSession.EnableKernelProvider(keywords, KernelTraceEventParser.Keywords.Profile);
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
        protected abstract string FileExtension { get; }

        protected TraceEventSession TraceEventSession { get; }

        protected DiagnoserActionParameters Details { get; }
        
        protected EtwProfilerConfig Config { get; }

        internal string FilePath { get; }

        protected Session(string sessionName, DiagnoserActionParameters details, EtwProfilerConfig config, DateTime creationTime)
        {
            Details = details;
            Config = config;
            FilePath = EnsureFolderExists(GetFilePath(details, creationTime));

            TraceEventSession = new TraceEventSession(sessionName, FilePath)
            {
                BufferSizeMB = config.BufferSizeInMb,
                CpuSampleIntervalMSec = config.CpuSampleIntervalInMiliseconds
            };

            Console.CancelKeyPress += OnConsoleCancelKeyPress;
            NativeWindowsConsoleHelper.OnExit += OnConsoleCancelKeyPress;
        }

        public void Dispose() => TraceEventSession.Dispose();

        internal void Stop()
        {
            TraceEventSession.Stop();

            Console.CancelKeyPress -= OnConsoleCancelKeyPress;
            NativeWindowsConsoleHelper.OnExit -= OnConsoleCancelKeyPress;
        }

        internal abstract Session EnableProviders();

        internal string MergeFiles(Session other) 
        {
            //  `other` is not used here because MergeInPlace expects .etl and .kernel.etl files in this folder
            // it searches for them and merges into a single file
            TraceEventSession.MergeInPlace(FilePath, TextWriter.Null);

            return FilePath;
        }

        private void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e) => Stop();

        private string GetFilePath(DiagnoserActionParameters details, DateTime creationTime)
        {
            var folderPath = details.Config.ArtifactsPath;

            folderPath = Path.Combine(folderPath, $"{creationTime:yyyyMMdd-hhmm}-{Process.GetCurrentProcess().Id}");
            
            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net461 vs netcoreapp2.1 etc
            if (details.Config.GetJobs().Select(job => job.Infrastructure.Toolchain).Distinct().Count() > 1)
                folderPath = Path.Combine(folderPath, details.BenchmarkCase.Job.Infrastructure.Toolchain.Name);

            if (!string.IsNullOrWhiteSpace(details.BenchmarkCase.Descriptor.Type.Namespace))
                folderPath = Path.Combine(folderPath, details.BenchmarkCase.Descriptor.Type.Namespace.Replace('.', Path.DirectorySeparatorChar));

            folderPath = Path.Combine(folderPath, FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type, includeNamespace: false));

            var fileName = FolderNameHelper.ToFolderName(FullNameProvider.GetMethodName(details.BenchmarkCase));

            return Path.Combine(folderPath, $"{fileName}{FileExtension}");
        }

        private string EnsureFolderExists(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return filePath;
        }
    }
}