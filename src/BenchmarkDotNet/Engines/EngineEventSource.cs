using System;
using System.Diagnostics.Tracing;

namespace BenchmarkDotNet.Engines
{
    [EventSource(Name = "BenchmarkDotNet.EngineEventSource")]
    public class EngineEventSource : EventSource
    {
        internal static readonly EngineEventSource Log = new EngineEventSource();

        [Event(1, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        internal void IterationStart(string jobId, string benchmarkName, IterationMode mode, IterationStage stage)
            => WriteEngineEvent(1, jobId, benchmarkName, mode, stage);
        
        [Event(2, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        internal void IterationStop(string jobId, string benchmarkName, IterationMode mode, IterationStage stage)
            => WriteEngineEvent(2, jobId, benchmarkName, mode, stage);

        [NonEvent]
        private unsafe void WriteEngineEvent(int eventId, string jobId, string benchmarkId, IterationMode mode, IterationStage stage)
        {
            fixed (char* jobIdPointer = jobId)
            fixed (char* benchmarkIdPointer = benchmarkId)
            {
                EventData* payload = stackalloc EventData[4];
                
                payload[0].Size = (jobId.Length + 1) * sizeof(char);
                payload[0].DataPointer = (IntPtr) jobIdPointer;
                
                payload[1].Size = (benchmarkId.Length + 1) * sizeof(char);
                payload[1].DataPointer = (IntPtr) benchmarkIdPointer;
                
                payload[2].Size = sizeof(int);
                payload[2].DataPointer = (IntPtr) (&mode);
                
                payload[3].Size = sizeof(int);
                payload[3].DataPointer = (IntPtr) (&stage);
                
                WriteEventCore(eventId, 4, payload);
            }
        }
    }
}