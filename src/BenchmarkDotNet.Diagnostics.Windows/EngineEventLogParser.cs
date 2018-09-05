using System;
using BenchmarkDotNet.Engines;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public sealed class EngineEventLogParser : TraceEventParser
    {
        private static volatile TraceEvent[] templates;
        
        public EngineEventLogParser(TraceEventSource source, bool dontRegister = false) : base(source, dontRegister) { }
        
        private static string ProviderName => EngineEventSource.Log.Name;

        private static Guid ProviderGuid => TraceEventProviders.GetEventSourceGuidFromName(ProviderName);

        public event Action<IterationEvent> BenchmarkIterationStart
        {
            add => source.RegisterEventTemplate(BenchmarkIterationStartTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.IterationStartEventId, ProviderGuid);
        }
        
        public event Action<IterationEvent> BenchmarkIterationStop
        {
            add => source.RegisterEventTemplate(BenchmarkIterationStopTemplate(value));
            remove => source.UnregisterEventTemplate(value, EngineEventSource.IterationStopEventId, ProviderGuid);
        }

        protected override string GetProviderName() { return ProviderName; }

        private static IterationEvent BenchmarkIterationStartTemplate(Action<IterationEvent> action)
        {                  // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
            return new IterationEvent(action, EngineEventSource.IterationStartEventId, 2, "Iteration", Guid.Empty, 1, "Start", ProviderGuid, ProviderName);
        }
        private static IterationEvent BenchmarkIterationStopTemplate(Action<IterationEvent> action)
        {                  // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
            return new IterationEvent(action, EngineEventSource.IterationStopEventId, 2, "Iteration", Guid.Empty, 2, "Stop", ProviderGuid, ProviderName);
        }
        
        protected override void EnumerateTemplates(Func<string, string, EventFilterResponse> eventsToObserve, Action<TraceEvent> callback)
        {
            if (templates == null)
            {
                var templates = new TraceEvent[2];
                templates[0] = BenchmarkIterationStartTemplate(null);
                templates[1] = BenchmarkIterationStopTemplate(null);
                EngineEventLogParser.templates = templates;
            }
            
            foreach (var template in templates)
                if (eventsToObserve == null || eventsToObserve(template.ProviderName, template.EventName) == EventFilterResponse.AcceptEvent)
                    callback(template);
        }
    }
}