using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class EtwProfiler : IDiagnoser
    {
        private readonly RunMode runMode;
        private readonly int bufferSizeInMb;

        private TraceEventSession kernelSession, userSession;
        private int count;

        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed on more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead</param>
        /// <param name="bufferSizeInMb">ETW session buffer size, in MB</param>
        public EtwProfiler(bool performExtraBenchmarksRun, int bufferSizeInMb)
        {
            runMode = performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            this.bufferSizeInMb = bufferSizeInMb;
        }

        public IEnumerable<string> Ids => new [] { nameof(EtwProfiler) };
        
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        
        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;
        
        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => runMode;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeAnythingElse)
            {
                StartTheSessions(parameters);
                
                Console.CancelKeyPress += OnConsoleCancelKeyPress;
                NativeWindowsConsoleHelper.OnExit += OnConsoleCancelKeyPress;
                
                EnableClrEvents();
                
                userSession.Clr.MethodInliningSucceeded += ClrOnMethodInliningSucceeded;
                
//                Task.Factory.StartNew((Action)(() => session.Source.Process()), TaskCreationOptions.LongRunning);                
//                AttachStackProfiles();
//                AttachPmcs();
            }
            else if (signal == HostSignal.BeforeOverheadRun)
            {
//                StartCalculatingOverheadPmcs();
            }
            else if (signal == HostSignal.AfterOverheadlRun)
            {
//                StopCalculatingOverheadPmcs();
            }
            else if (signal == HostSignal.BeforeActualRun)
            {
//                StartCalculatingWorkloadPmcs();
            }
            else if (signal == HostSignal.AfterActualRun)
            {
//                StopCalculatingActualPmcs();

                var fileName = userSession.FileName;

                kernelSession.Dispose();
                userSession.Dispose();
                
                TraceEventSession.MergeInPlace(fileName, Console.Out);

//                TraceEventSession.Merge(new [] { fileName}, fileName + ".merged.etl", TraceEventMergeOptions.Compress);

                Console.CancelKeyPress -= OnConsoleCancelKeyPress;
                NativeWindowsConsoleHelper.OnExit -= OnConsoleCancelKeyPress;


                return;
            }
            else if (signal == HostSignal.SeparateLogic)
            {
                return; // ignore it
            }

//            Marker(signal);
        }

        private void ClrOnMethodInliningSucceeded(MethodJitInliningSucceededTraceData obj)
        {
            count++;
        }

        private void StartTheSessions(DiagnoserActionParameters parameters)
        {
            kernelSession = new TraceEventSession(
                KernelTraceEventParser.KernelSessionName,
                Path.Combine(parameters.Config.ArtifactsPath, $"{FullNameProvider.GetBenchmarkName(parameters.BenchmarkCase)}.kernel.etl"))
            {
                BufferSizeMB = bufferSizeInMb
            };
            
             userSession = new TraceEventSession(
                 FullNameProvider.GetBenchmarkName(parameters.BenchmarkCase), 
                 Path.Combine(parameters.Config.ArtifactsPath, $"{FullNameProvider.GetBenchmarkName(parameters.BenchmarkCase)}.etl"))
             {
                 BufferSizeMB = bufferSizeInMb
             };
        }

        private void EnableClrEvents()
        {
            userSession.EnableProvider(
                ClrTraceEventParser.ProviderGuid,
                TraceEventLevel.Verbose,
                (ulong) (ClrTraceEventParser.Keywords.Exception
                         | ClrTraceEventParser.Keywords.GC
                         | ClrTraceEventParser.Keywords.Jit
                         | ClrTraceEventParser.Keywords.JitTracing // for the inlining events
                         | ClrTraceEventParser.Keywords.Loader
                         | ClrTraceEventParser.Keywords.NGen));
//                new TraceEventProviderOptions() { StacksEnabled = true });

            kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Profile);
        }


        private void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            userSession?.Dispose();
            kernelSession?.Dispose();
        }

        public void ProcessResults(DiagnoserResults results) { }

        public void DisplayResults(ILogger logger) => logger.WriteLine($"inlined!!! {count}");

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();
    }
}