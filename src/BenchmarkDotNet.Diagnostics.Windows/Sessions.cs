using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    internal class UserSession : Session
    {
        public UserSession(DiagnoserActionParameters details, int bufferSizeInMb)
            : base(FullNameProvider.GetBenchmarkName(details.BenchmarkCase), details, bufferSizeInMb)
        {
        }

        protected override string FileExtension => ".etl";

        internal override Session EnableProviders()
        {
            TraceEventSession.EnableProvider(
                ClrTraceEventParser.ProviderGuid,
                TraceEventLevel.Verbose,
                (ulong) (ClrTraceEventParser.Keywords.Exception
                         | ClrTraceEventParser.Keywords.GC
                         | ClrTraceEventParser.Keywords.Jit
                         | ClrTraceEventParser.Keywords.JitTracing // for the inlining events
                         | ClrTraceEventParser.Keywords.Loader
                         | ClrTraceEventParser.Keywords.NGen),
                new TraceEventProviderOptions { StacksEnabled = false }); // stacks are too expensive for our purposes

            return this;
        }
    }

    internal class KernelSession : Session
    {
        public KernelSession(DiagnoserActionParameters details, int bufferSizeInMb) : base(KernelTraceEventParser.KernelSessionName, details, bufferSizeInMb) { }
        
        protected override string FileExtension => ".kernel.etl";

        internal override Session EnableProviders()
        {
            if (!Details.Config.GetHardwareCounters().Any())
            {
                TraceEventSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Profile); // enable CPU stacks only
                
                return this;
            }

            TraceEventSession.EnableKernelProvider(KernelTraceEventParser.Keywords.PMCProfile | KernelTraceEventParser.Keywords.Profile); // enable PMCs and CPU stacks

            var counters = Details.Config
                .GetHardwareCounters()
                .Select(counter => HardwareCounters.FromCounter(counter, info => info.MaxInterval )) // for this diagnoser we want the biggest interval to have smallest overhead
                .ToArray();

            HardwareCounters.Enable(counters);

            return this;
        }
    }
    
    internal abstract class Session : IDisposable
    {
        protected abstract string FileExtension { get; }

        protected TraceEventSession TraceEventSession { get; }

        protected DiagnoserActionParameters Details { get; }

        private string FilePath { get; }

        protected Session(string sessionName, DiagnoserActionParameters details, int bufferSizeInMb)
        {
            Details = details;
            FilePath = EnsureFolderExists(GetFilePath(details));

            TraceEventSession = new TraceEventSession(sessionName, FilePath)
            {
                BufferSizeMB = bufferSizeInMb
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

        private string GetFilePath(DiagnoserActionParameters details)
        {
            var folderPath = details.Config.ArtifactsPath;

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