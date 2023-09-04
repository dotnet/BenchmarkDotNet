using System;
using System.Diagnostics.Tracing;
using BenchmarkDotNet.Engines;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows.Tracing
{
    public sealed class EngineEventLogParser : TraceEventParser
    {
        private static volatile TraceEvent[]? templates;

        public EngineEventLogParser(TraceEventSource source, bool dontRegister = false) : base(source, dontRegister) { }

        private static string ProviderName => EngineEventSource.Log.Name;

        private static Guid ProviderGuid => TraceEventProviders.GetEventSourceGuidFromName(ProviderName);

        public event Action<IterationEvent> BenchmarkStart
        {
            add => source.RegisterEventTemplate(BenchmarkStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.BenchmarkStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> BenchmarkStop
        {
            add => source.RegisterEventTemplate(BenchmarkStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.BenchmarkStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> OverheadJittingStart
        {
            add => source.RegisterEventTemplate(OverheadJittingStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.OverheadJittingStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> OverheadJittingStop
        {
            add => source.RegisterEventTemplate(OverheadJittingStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.OverheadJittingStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadJittingStart
        {
            add => source.RegisterEventTemplate(WorkloadJittingStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadJittingStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadJittingStop
        {
            add => source.RegisterEventTemplate(WorkloadJittingStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadJittingStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadPilotStart
        {
            add => source.RegisterEventTemplate(WorkloadPilotStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadPilotStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadPilotStop
        {
            add => source.RegisterEventTemplate(WorkloadPilotStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadPilotStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> OverheadWarmupStart
        {
            add => source.RegisterEventTemplate(OverheadWarmupStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.OverheadWarmupStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> OverheadWarmupStop
        {
            add => source.RegisterEventTemplate(OverheadWarmupStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.OverheadWarmupStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadWarmupStart
        {
            add => source.RegisterEventTemplate(WorkloadWarmupStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadWarmupStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadWarmupStop
        {
            add => source.RegisterEventTemplate(WorkloadWarmupStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadWarmupStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> OverheadActualStart
        {
            add => source.RegisterEventTemplate(OverheadActualStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.OverheadActualStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> OverheadActualStop
        {
            add => source.RegisterEventTemplate(OverheadActualStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.OverheadActualStopEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadActualStart
        {
            add => source.RegisterEventTemplate(WorkloadActualStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadActualStartEventId, ProviderGuid);
        }

        public event Action<IterationEvent> WorkloadActualStop
        {
            add => source.RegisterEventTemplate(WorkloadActualStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.WorkloadActualStopEventId, ProviderGuid);
        }

        protected override string GetProviderName() { return ProviderName; }

        private static IterationEvent BenchmarkStartTemplate(Action<IterationEvent>? action)
        {                  // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
            return new IterationEvent(action, EngineEventSource.BenchmarkStartEventId, (int)EngineEventSource.Tasks.Benchmark, nameof(EngineEventSource.Tasks.Benchmark), Guid.Empty, (int)EventOpcode.Start, nameof(EventOpcode.Start), ProviderGuid, ProviderName);
        }

        private static IterationEvent BenchmarkStopTemplate(Action<IterationEvent>? action)
        {                  // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
            return new IterationEvent(action, EngineEventSource.BenchmarkStopEventId, (int)EngineEventSource.Tasks.Benchmark, nameof(EngineEventSource.Tasks.Benchmark), Guid.Empty, (int)EventOpcode.Stop, nameof(EventOpcode.Stop), ProviderGuid, ProviderName);
        }

        private static IterationEvent OverheadJittingStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.OverheadJittingStartEventId, EngineEventSource.Tasks.OverheadJitting);

        private static IterationEvent OverheadJittingStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.OverheadJittingStopEventId, EngineEventSource.Tasks.OverheadJitting);

        private static IterationEvent WorkloadJittingStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.WorkloadJittingStartEventId, EngineEventSource.Tasks.WorkloadJitting);

        private static IterationEvent WorkloadJittingStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.WorkloadJittingStopEventId, EngineEventSource.Tasks.WorkloadJitting);

        private static IterationEvent WorkloadPilotStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.WorkloadPilotStartEventId, EngineEventSource.Tasks.WorkloadPilot);

        private static IterationEvent WorkloadPilotStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.WorkloadPilotStopEventId, EngineEventSource.Tasks.WorkloadPilot);

        private static IterationEvent OverheadWarmupStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.OverheadWarmupStartEventId, EngineEventSource.Tasks.OverheadWarmup);

        private static IterationEvent OverheadWarmupStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.OverheadWarmupStopEventId, EngineEventSource.Tasks.OverheadWarmup);

        private static IterationEvent WorkloadWarmupStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.WorkloadWarmupStartEventId, EngineEventSource.Tasks.WorkloadWarmup);

        private static IterationEvent WorkloadWarmupStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.WorkloadWarmupStopEventId, EngineEventSource.Tasks.WorkloadWarmup);

        private static IterationEvent OverheadActualStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.OverheadActualStartEventId, EngineEventSource.Tasks.OverheadActual);

        private static IterationEvent OverheadActualStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.OverheadActualStopEventId, EngineEventSource.Tasks.OverheadActual);

        private static IterationEvent WorkloadActualStartTemplate(Action<IterationEvent>? action)
            => CreateIterationStartTemplate(action, EngineEventSource.WorkloadActualStartEventId, EngineEventSource.Tasks.WorkloadActual);

        private static IterationEvent WorkloadActualStopTemplate(Action<IterationEvent>? action)
            => CreateIterationStopTemplate(action, EngineEventSource.WorkloadActualStopEventId, EngineEventSource.Tasks.WorkloadActual);

        private static IterationEvent CreateIterationStartTemplate(Action<IterationEvent>? action, int eventId, EventTask eventTask)
        {                  // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
            return new IterationEvent(action, eventId, (int)eventTask, eventTask.ToString(), Guid.Empty, (int)EventOpcode.Start, nameof(EventOpcode.Start), ProviderGuid, ProviderName);
        }

        private static IterationEvent CreateIterationStopTemplate(Action<IterationEvent>? action, int eventId, EventTask eventTask)
        {                  // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
            return new IterationEvent(action, eventId, (int)eventTask, eventTask.ToString(), Guid.Empty, (int)EventOpcode.Stop, nameof(EventOpcode.Stop), ProviderGuid, ProviderName);
        }

        protected override void EnumerateTemplates(Func<string, string, EventFilterResponse>? eventsToObserve, Action<TraceEvent> callback)
        {
            if (templates == null)
            {
                var templates = new TraceEvent[16];
                templates[0] = BenchmarkStartTemplate(null);
                templates[1] = BenchmarkStopTemplate(null);
                templates[2] = OverheadJittingStartTemplate(null);
                templates[3] = OverheadJittingStopTemplate(null);
                templates[4] = WorkloadJittingStartTemplate(null);
                templates[5] = WorkloadJittingStopTemplate(null);
                templates[6] = WorkloadPilotStartTemplate(null);
                templates[7] = WorkloadPilotStopTemplate(null);
                templates[8] = OverheadWarmupStartTemplate(null);
                templates[9] = OverheadWarmupStopTemplate(null);
                templates[10] = OverheadActualStartTemplate(null);
                templates[11] = OverheadActualStopTemplate(null);
                templates[12] = WorkloadWarmupStartTemplate(null);
                templates[13] = WorkloadWarmupStopTemplate(null);
                templates[14] = WorkloadActualStartTemplate(null);
                templates[15] = WorkloadActualStopTemplate(null);
                EngineEventLogParser.templates = templates;
            }

            foreach (var template in templates)
                if (eventsToObserve == null || eventsToObserve(template.ProviderName, template.EventName) == EventFilterResponse.AcceptEvent)
                    callback(template);
        }
    }
}