using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class InliningDiagnoser : ETWDiagnoser, IDiagnoser
    {
        private readonly LogCapture logger = new LogCapture();        
        private readonly ConcurrentDictionary<int, object> statsPerProcess = new ConcurrentDictionary<int, object>();
        private TraceEventSession session;

        public void Start(Benchmark benchmark)
        {
            ProcessIdsUsedInRuns.Clear();
            statsPerProcess.Clear();

            var sessionName = GetSessionName("JitTracing", benchmark, benchmark.Parameters);
            session = new TraceEventSession(sessionName);
            session.EnableProvider(ClrTraceEventParser.ProviderGuid,
                                   TraceEventLevel.Verbose,
                                   (ulong)(ClrTraceEventParser.Keywords.JitTracing));

            // The ETW collection thread starts receiving events immediately, but we only
            // start aggregating them after ProcessStarted is called and we know which process
            // (or processes) we should be monitoring. Communication between the benchmark thread
            // and the ETW collection thread is through the statsPerProcess concurrent dictionary
            // and through the TraceEventSession class, which is thread-safe.
            Task.Factory.StartNew(() => StartProcessingEvents(benchmark), TaskCreationOptions.LongRunning);
        }

        public void Stop(Benchmark benchmark, BenchmarkReport report)
        {
            // ETW real-time sessions receive events with a slight delay. Typically it
            // shouldn't be more than a few seconds. This increases the likelihood that
            // all relevant events are processed by the collection thread by the time we
            // are done with the benchmark.
            Thread.Sleep(TimeSpan.FromSeconds(3));

            session.Dispose();
        }

        public void ProcessStarted(Process process)
        {
            ProcessIdsUsedInRuns.Add(process.Id);
            statsPerProcess.TryAdd(process.Id, null);
        }

        public void AfterBenchmarkHasRun(Benchmark benchmark, Process process)
        {
            // Do nothing
        }

        public void ProcessStopped(Process process)
        {
            // Do nothing
        }

        public void DisplayResults(ILogger outputLogger)
        {
            if (logger.CapturedOutput.Count > 0)
                outputLogger.WriteLineHeader(new string('-', 20));
            foreach (var line in logger.CapturedOutput)
                outputLogger.Write(line.Kind, line.Text);
        }

        private void StartProcessingEvents(Benchmark benchmark)
        {
            var expected = benchmark.Target.Method.DeclaringType.Namespace  + "." +
                           benchmark.Target.Method.DeclaringType.Name;

            logger.WriteLine();
            logger.WriteLineHeader(new string('-', 20));
            logger.WriteLineInfo($"{benchmark.FullInfo}");
            logger.WriteLineHeader(new string('-', 20));

            session.Source.Clr.MethodInliningSucceeded += jitData =>
            {
                // Inliner = the parent method (the inliner calls the inlinee)
                // Inlinee = the method that is going to be "inlined" inside the inliner (it's caller)                
                object _ignored;
                if (statsPerProcess.TryGetValue(jitData.ProcessID, out _ignored))
                {
                    var shouldPrint = jitData.InlinerNamespace == expected ||
                                      jitData.InlineeNamespace == expected;
                    if (shouldPrint)
                    {
                        logger.WriteLineHelp($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                        logger.WriteLineHelp($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                        logger.WriteLineHeader(new string('-', 20));
                    }
                }
            };

            session.Source.Clr.MethodInliningFailed += jitData =>
            {
                object _ignored;
                if (statsPerProcess.TryGetValue(jitData.ProcessID, out _ignored))
                {
                    var shouldPrint = jitData.InlinerNamespace == expected ||
                                      jitData.InlineeNamespace == expected;
                    if (shouldPrint)
                    {
                        logger.WriteLineError($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                        logger.WriteLineError($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                        // See https://blogs.msdn.microsoft.com/clrcodegeneration/2009/10/21/jit-etw-inlining-event-fail-reasons/
                        logger.WriteLineError($"Fail Reason: {jitData.FailReason}");
                        logger.WriteLineHeader(new string('-', 20));
                    }
                }
            };

            session.Source.Process();
        }
    }
}
