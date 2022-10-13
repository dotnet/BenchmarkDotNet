using System;
using System.Collections.Concurrent;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class EtwDiagnoser<TStats> where TStats : new()
    {
        internal readonly LogCapture Logger = new LogCapture();
        protected readonly Dictionary<BenchmarkCase, int> BenchmarkToProcess = new Dictionary<BenchmarkCase, int>();
        protected readonly ConcurrentDictionary<int, TStats> StatsPerProcess = new ConcurrentDictionary<int, TStats>();

        public virtual RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.ExtraRun;

        public virtual IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public virtual IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        protected TraceEventSession Session { get; private set; }

        protected abstract ulong EventType { get; }

        protected abstract string SessionNamePrefix { get; }

        protected void Start(DiagnoserActionParameters parameters)
        {
            Clear();

            BenchmarkToProcess.Add(parameters.BenchmarkCase, parameters.Process.Id);
            StatsPerProcess.TryAdd(parameters.Process.Id, GetInitializedStats(parameters));

            // Important: Must wire-up clean-up events prior to acquiring IDisposable instance (Session property)
            // This is in effect the inverted sequence of actions in the Stop() method.
            Console.CancelKeyPress += OnConsoleCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            Session = CreateSession(parameters.BenchmarkCase);

            EnableProvider();

            AttachToEvents(Session, parameters.BenchmarkCase);

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

        protected virtual TraceEventSession CreateSession(BenchmarkCase benchmarkCase)
             => new TraceEventSession(GetSessionName(SessionNamePrefix, benchmarkCase, benchmarkCase.Parameters));

        protected virtual void EnableProvider()
        {
            Session.EnableProvider(
                ClrTraceEventParser.ProviderGuid,
                TraceEventLevel.Verbose,
                EventType);
        }

        protected abstract void AttachToEvents(TraceEventSession traceEventSession, BenchmarkCase benchmarkCase);

        protected void Stop()
        {
            WaitForDelayedEvents();

            Session.Dispose();

            Console.CancelKeyPress -= OnConsoleCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }

        private void Clear()
        {
            BenchmarkToProcess.Clear();
            StatsPerProcess.Clear();
        }

        private void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e) => Session?.Dispose();

        private void OnProcessExit(object sender, EventArgs e) => Session?.Dispose();

        private static string GetSessionName(string prefix, BenchmarkCase benchmarkCase, ParameterInstances parameters = null)
        {
            if (parameters != null && parameters.Items.Count > 0)
                return $"{prefix}-{benchmarkCase.FolderInfo}-{parameters.FolderInfo}";
            return $"{prefix}-{benchmarkCase.FolderInfo}";
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
