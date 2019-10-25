using System;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading;

namespace BenchmarkDotNet.Engines {
    [EventSource(Name = BenchmarkDotNetEventSource.SourceName)]
    public class BenchmarkDotNetEventSource : EventSource
    {
        // this name will be used as "provider" name with dotnet-counters
        // ex: dotnet-counters monitor -p <pid> BenchmarkDotNet.Counters
        const string SourceName = "BenchmarkDotNet.Counters";

        private int executedBenchmarkCount;
        private int selectedBenchmarkCount;


        private object selectedBenchmarkCounter;
        private object executedBenchmarkCounter;

        public static void Initialize()
        {
            if (Instance != null)
                throw new InvalidOperationException($"{nameof(BenchmarkDotNetEventSource)} can't be initialized more than once");

            Instance = new BenchmarkDotNetEventSource();
        }

        public static BenchmarkDotNetEventSource Instance { get; private set; }

        public BenchmarkDotNetEventSource()
            : base(SourceName, EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                var type = Type.GetType("System.Diagnostics.Tracing.PollingCounter");
                if (type == null)
                {
                    return;
                }

                var property = type.GetProperty("DisplayName");
                if (property == null)
                {
                    return;
                }
                CreateCounters(type, property);
            }
        }

        private void CreateCounters(Type pollingCounter, PropertyInfo displayNameProperty)
        {
            // NOTE: These counters will NOT be disposed on disable command because we may be introducing
            // a race condition by doing that. We still want to create these lazily so that we aren't adding
            // overhead by at all times even when counters aren't enabled.

            double GetSelectedBenchmark() => selectedBenchmarkCount;
            object[] selectedBenchmarkArgs = {"selected-benchmark-count", this, (Func<double>)GetSelectedBenchmark};
            selectedBenchmarkCounter = Activator.CreateInstance(pollingCounter, selectedBenchmarkArgs);
            displayNameProperty.SetValue(selectedBenchmarkCounter, "Selected benchmark count");

            double GetExecutedBenchmarkCount() => executedBenchmarkCount;
            object[] executedBenchmarkArgs = { "executed-benchmark-count", this, (Func<double>) GetExecutedBenchmarkCount };
            executedBenchmarkCounter = Activator.CreateInstance(pollingCounter, executedBenchmarkArgs);
            displayNameProperty.SetValue(executedBenchmarkCounter, "Executed benchmark count");
        }

        internal void SetSelectedBenchmarkCount(int count)
        {
            Interlocked.Exchange(ref selectedBenchmarkCount, count);
        }

        public void IncrementExecutedBenchmarkCount()
        {
            Interlocked.Increment(ref executedBenchmarkCount);
        }
    }
}