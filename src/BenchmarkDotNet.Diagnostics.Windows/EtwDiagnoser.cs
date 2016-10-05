using System;
using System.Collections.Concurrent;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class EtwDiagnoser<TStats> where TStats : new()
    {
        protected readonly LogCapture Logger = new LogCapture();
        protected readonly List<int> ProcessIdsUsedInRuns = new List<int>();
        protected readonly ConcurrentDictionary<int, TStats> StatsPerProcess = new ConcurrentDictionary<int, TStats>();

        private TraceEventSession session;

        protected abstract ClrTraceEventParser.Keywords EventType { get; }

        protected abstract string SessionNamePrefix { get; }

        protected void Start(Process process, Benchmark benchmark)
        {
            Cleanup();

            ProcessIdsUsedInRuns.Add(process.Id);
            StatsPerProcess.TryAdd(process.Id, new TStats());

            AttachToEvents(CreateSession(benchmark), benchmark);

            EnableProvider();

            // The ETW collection thread starts receiving events immediately, but we only
            // start aggregating them after ProcessStarted is called and we know which process
            // (or processes) we should be monitoring. Communication between the benchmark thread
            // and the ETW collection thread is through the statsPerProcess concurrent dictionary
            // and through the TraceEventSession class, which is thread-safe.
            var task = Task.Factory.StartNew((Action)(() => session.Source.Process()), TaskCreationOptions.LongRunning);

            // wait until the processing has started, block by then so we don't loose any 
            // information (very important for jit-related things)
            WaitUntilStarted(task); 
        }

        protected void Stop()
        {
            // ETW real-time sessions receive events with a slight delay. Typically it
            // shouldn't be more than a few seconds. This increases the likelihood that
            // all relevant events are processed by the collection thread by the time we
            // are done with the benchmark.
            Thread.Sleep(TimeSpan.FromSeconds(3));

            session.Dispose();
        }

        protected abstract void AttachToEvents(TraceEventSession traceEventSession, Benchmark benchmark);

        private void Cleanup()
        {
            ProcessIdsUsedInRuns.Clear();
            StatsPerProcess.Clear();
        }

        private TraceEventSession CreateSession(Benchmark benchmark)
        {
            var sessionName = GetSessionName(SessionNamePrefix, benchmark, benchmark.Parameters);

            session = new TraceEventSession(sessionName);

            return session;
        }

        private void EnableProvider()
        {
            session.EnableProvider(
                ClrTraceEventParser.ProviderGuid,
                TraceEventLevel.Verbose,
                (ulong)(EventType));
        }

        private static string GetSessionName(string prefix, Benchmark benchmark, ParameterInstances parameters = null)
        {
            if (parameters != null && parameters.Items.Count > 0)
                return $"{prefix}-{benchmark.FolderInfo}-{parameters.FolderInfo}";
            return $"{prefix}-{benchmark.FolderInfo}";
        }

        private static void WaitUntilStarted(Task task)
        {
            while (task.Status == TaskStatus.Created
                || task.Status == TaskStatus.WaitingForActivation
                || task.Status == TaskStatus.WaitingToRun)
            {
                Thread.Sleep(10);
            }
        }
    }
}
