using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class EtwProfiler : IDiagnoser
    {
        private readonly RunMode runMode;
        private readonly int bufferSizeInMb;
        private readonly Dictionary<BenchmarkCase, string> benchmarkToEtlFile;

        private Session kernelSession, userSession;

        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed on more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead</param>
        /// <param name="bufferSizeInMb">ETW session buffer size, in MB</param>
        public EtwProfiler(bool performExtraBenchmarksRun, int bufferSizeInMb)
        {
            runMode = performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            this.bufferSizeInMb = bufferSizeInMb;
            benchmarkToEtlFile = new Dictionary<BenchmarkCase, string>();
        }

        public IEnumerable<string> Ids => new [] { nameof(EtwProfiler) };
        
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        
        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;
        
        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => runMode;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.SeparateLogic)
                return; // ignore it
            
            if (signal == HostSignal.BeforeAnythingElse)
            {
                userSession = new UserSession(parameters, bufferSizeInMb).EnableProviders();
                kernelSession = new KernelSession(parameters, bufferSizeInMb).EnableProviders();
            }
            
            //            Marker(signal);
            
            if (signal == HostSignal.AfterActualRun)
            {
                kernelSession.Stop();
                userSession.Stop();
                
                userSession.MergeFiles(kernelSession);
                
                kernelSession.Dispose();
                userSession.Dispose();
            }
        }
        
        public void ProcessResults(DiagnoserResults results) { }

        public void DisplayResults(ILogger logger)
        {
            if (!benchmarkToEtlFile.Any())
                return;
            
            logger.WriteInfo($"Exported {benchmarkToEtlFile.Count} etl files. Some of them:");
            foreach (string file in benchmarkToEtlFile.Values.Take(10))
                logger.WriteLineInfo(file);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            if (TraceEventSession.IsElevated() != true)
                yield return new ValidationError(true, "Must be elevated (Admin) to use Hardware Counters to use ETW Kernel Session.");
        }
    }
}