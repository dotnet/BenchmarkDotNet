using System;
using System.Collections.Concurrent;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class EtwDiagnoser<TStats> where TStats : new()
    {
        protected readonly LogCapture Logger = new LogCapture();
        protected readonly Dictionary<Benchmark, int> BenchmarkToProcess = new Dictionary<Benchmark, int>();
        protected readonly ConcurrentDictionary<int, TStats> StatsPerProcess = new ConcurrentDictionary<int, TStats>();

        public virtual RunMode GetRunMode(Benchmark benchmark) => RunMode.ExtraRun;

        public virtual IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        protected TraceEventSession Session { get; private set; }

        protected abstract ulong EventType { get; }

        protected abstract string SessionNamePrefix { get; }

        protected void Start(DiagnoserActionParameters parameters)
        {
            Clear();

            BenchmarkToProcess.Add(parameters.Benchmark, parameters.Process.Id);
            StatsPerProcess.TryAdd(parameters.Process.Id, GetInitializedStats(parameters));

            Session = CreateSession(parameters.Benchmark);

            EnableProvider();

            AttachToEvents(Session, parameters.Benchmark);

            // The ETW collection thread starts receiving events immediately, but we only
            // start aggregating them after ProcessStarted is called and we know which process
            // (or processes) we should be monitoring. Communication between the benchmark thread
            // and the ETW collection thread is through the statsPerProcess concurrent dictionary
            // and through the TraceEventSession class, which is thread-safe.
            var task = Task.Factory.StartNew((Action)(() => Session.Source.Process()), TaskCreationOptions.LongRunning);

            // wait until the processing has started, block by then so we don't loose any 
            // information (very important for jit-related things)
            WaitUntilStarted(task);
        }

        protected virtual TStats GetInitializedStats(DiagnoserActionParameters parameters) => new TStats();

        protected virtual TraceEventSession CreateSession(Benchmark benchmark)
             => new TraceEventSession(GetSessionName(SessionNamePrefix, benchmark, benchmark.Parameters));

        protected virtual void EnableProvider()
        {
            Session.EnableProvider(
                ClrTraceEventParser.ProviderGuid,
                TraceEventLevel.Verbose,
                EventType);
        }

        protected abstract void AttachToEvents(TraceEventSession traceEventSession, Benchmark benchmark);

        protected void Stop()
        {
            WaitForDelayedEvents();

            Session.Dispose();
        }

        private void Clear()
        {
            BenchmarkToProcess.Clear();
            StatsPerProcess.Clear();
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

        /// <summary>
        /// ETW real-time sessions receive events with a slight delay. Typically it
        /// shouldn't be more than a few seconds. This increases the likelihood that
        /// all relevant events are processed by the collection thread by the time we
        /// are done with the benchmark.
        /// </summary>
        private static void WaitForDelayedEvents()
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}
