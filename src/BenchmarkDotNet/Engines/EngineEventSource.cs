using System;
using System.Diagnostics.Tracing;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    [EventSource(Name = "BenchmarkDotNet.EngineEventSource")]
    public class EngineEventSource : EventSource
    {
        [PublicAPI] public const int IterationStartEventId = 1;
        [PublicAPI] public const int IterationStopEventId = 2;

        /// <summary>
        /// "If you use tasks and opcodes, you should name your method the task name concatenated with the opcode name."
        /// https://github.com/Microsoft/dotnet-samples/blob/6f2414148e33740c29116138e8bcef28364fafa8/Microsoft.Diagnostics.Tracing/EventSource/EventSource/20_CustomizedEventSource.cs#L37
        /// </summary>
        private const EventTask IterationTask = (EventTask)1;

        internal static readonly EngineEventSource Log = new EngineEventSource();

        [Event(IterationStartEventId, Level = EventLevel.Informational, Task = IterationTask, Opcode = EventOpcode.Start)]
        internal void IterationStart(string jobId, string benchmarkName, IterationMode mode, IterationStage stage)
            => WriteEngineEvent(IterationStartEventId, jobId, benchmarkName, mode, stage);
        
        [Event(IterationStopEventId, Level = EventLevel.Informational, Task = IterationTask, Opcode = EventOpcode.Stop)]
        internal void IterationStop(string jobId, string benchmarkName, IterationMode mode, IterationStage stage)
            => WriteEngineEvent(IterationStopEventId, jobId, benchmarkName, mode, stage);

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