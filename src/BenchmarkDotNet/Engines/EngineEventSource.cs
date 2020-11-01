using System;
using System.Diagnostics.Tracing;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    [EventSource(Name = EngineEventSource.SourceName)]
    public class EngineEventSource : EventSource
    {
        public const string SourceName = "BenchmarkDotNet.EngineEventSource";

        [PublicAPI] public const int BenchmarkStartEventId = 1;
        [PublicAPI] public const int BenchmarkStopEventId = 2;
        [PublicAPI] public const int OverheadJittingStartEventId = 3;
        [PublicAPI] public const int OverheadJittingStopEventId = 4;
        [PublicAPI] public const int WorkloadJittingStartEventId = 5;
        [PublicAPI] public const int WorkloadJittingStopEventId = 6;
        [PublicAPI] public const int WorkloadPilotStartEventId = 7;
        [PublicAPI] public const int WorkloadPilotStopEventId = 8;
        [PublicAPI] public const int OverheadWarmupStartEventId = 9;
        [PublicAPI] public const int OverheadWarmupStopEventId = 10;
        [PublicAPI] public const int OverheadActualStartEventId = 11;
        [PublicAPI] public const int OverheadActualStopEventId = 12;
        [PublicAPI] public const int WorkloadWarmupStartEventId = 13;
        [PublicAPI] public const int WorkloadWarmupStopEventId = 14;
        [PublicAPI] public const int WorkloadActualStartEventId = 15;
        [PublicAPI] public const int WorkloadActualStopEventId = 16;

        public class Tasks
        {
            [PublicAPI] public const EventTask Benchmark = (EventTask)1;
            [PublicAPI] public const EventTask OverheadJitting = (EventTask)2;
            [PublicAPI] public const EventTask WorkloadJitting = (EventTask)3;
            [PublicAPI] public const EventTask WorkloadPilot = (EventTask)4;
            [PublicAPI] public const EventTask OverheadWarmup = (EventTask)5;
            [PublicAPI] public const EventTask OverheadActual = (EventTask)6;
            [PublicAPI] public const EventTask WorkloadWarmup = (EventTask)7;
            [PublicAPI] public const EventTask WorkloadActual = (EventTask)8;
        }

        internal static readonly EngineEventSource Log = new EngineEventSource();

        private EngineEventSource() { }

        [Event(BenchmarkStartEventId, Level = EventLevel.Informational, Task = Tasks.Benchmark, Opcode = EventOpcode.Start)]
        internal void BenchmarkStart(string benchmarkName) => WriteEvent(BenchmarkStartEventId, benchmarkName);

        [Event(BenchmarkStopEventId, Level = EventLevel.Informational, Task = Tasks.Benchmark, Opcode = EventOpcode.Stop)]
        internal void BenchmarkStop(string benchmarkName) => WriteEvent(BenchmarkStopEventId, benchmarkName);

        [Event(OverheadJittingStartEventId, Level = EventLevel.Informational, Task = Tasks.OverheadJitting, Opcode = EventOpcode.Start)]
        internal void OverheadJittingStart(long totalOperations) => WriteEvent(OverheadJittingStartEventId, totalOperations);

        [Event(OverheadJittingStopEventId, Level = EventLevel.Informational, Task = Tasks.OverheadJitting, Opcode = EventOpcode.Stop)]
        internal void OverheadJittingStop(long totalOperations) => WriteEvent(OverheadJittingStopEventId, totalOperations);

        [Event(WorkloadJittingStartEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadJitting, Opcode = EventOpcode.Start)]
        internal void WorkloadJittingStart(long totalOperations) => WriteEvent(WorkloadJittingStartEventId, totalOperations);

        [Event(WorkloadJittingStopEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadJitting, Opcode = EventOpcode.Stop)]
        internal void WorkloadJittingStop(long totalOperations) => WriteEvent(WorkloadJittingStopEventId, totalOperations);

        [Event(WorkloadPilotStartEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadPilot, Opcode = EventOpcode.Start)]
        internal void WorkloadPilotStart(long totalOperations) => WriteEvent(WorkloadPilotStartEventId, totalOperations);

        [Event(WorkloadPilotStopEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadPilot, Opcode = EventOpcode.Stop)]
        internal void WorkloadPilotStop(long totalOperations) => WriteEvent(WorkloadPilotStopEventId, totalOperations);

        [Event(OverheadWarmupStartEventId, Level = EventLevel.Informational, Task = Tasks.OverheadWarmup, Opcode = EventOpcode.Start)]
        internal void OverheadWarmupStart(long totalOperations) => WriteEvent(OverheadWarmupStartEventId, totalOperations);

        [Event(OverheadWarmupStopEventId, Level = EventLevel.Informational, Task = Tasks.OverheadWarmup, Opcode = EventOpcode.Stop)]
        internal void OverheadWarmupStop(long totalOperations) => WriteEvent(OverheadWarmupStopEventId, totalOperations);

        [Event(OverheadActualStartEventId, Level = EventLevel.Informational, Task = Tasks.OverheadActual, Opcode = EventOpcode.Start)]
        internal void OverheadActualStart(long totalOperations) => WriteEvent(OverheadActualStartEventId, totalOperations);

        [Event(OverheadActualStopEventId, Level = EventLevel.Informational, Task = Tasks.OverheadActual, Opcode = EventOpcode.Stop)]
        internal void OverheadActualStop(long totalOperations) => WriteEvent(OverheadActualStopEventId, totalOperations);

        [Event(WorkloadWarmupStartEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadWarmup, Opcode = EventOpcode.Start)]
        internal void WorkloadWarmupStart(long totalOperations) => WriteEvent(WorkloadWarmupStartEventId, totalOperations);

        [Event(WorkloadWarmupStopEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadWarmup, Opcode = EventOpcode.Stop)]
        internal void WorkloadWarmupStop(long totalOperations) => WriteEvent(WorkloadWarmupStopEventId, totalOperations);

        [Event(WorkloadActualStartEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadActual, Opcode = EventOpcode.Start)]
        internal void WorkloadActualStart(long totalOperations) => WriteEvent(WorkloadActualStartEventId, totalOperations);

        [Event(WorkloadActualStopEventId, Level = EventLevel.Informational, Task = Tasks.WorkloadActual, Opcode = EventOpcode.Stop)]
        internal void WorkloadActualStop(long totalOperations) => WriteEvent(WorkloadActualStopEventId, totalOperations);

        [NonEvent]
        internal void IterationStart(IterationMode mode, IterationStage stage, long totalOperations)
        {
            switch (stage)
            {
                case IterationStage.Jitting when mode == IterationMode.Overhead:
                    OverheadJittingStart(totalOperations);
                    break;
                case IterationStage.Jitting when mode == IterationMode.Workload:
                    WorkloadJittingStart(totalOperations);
                    break;
                case IterationStage.Pilot when mode == IterationMode.Workload:
                    WorkloadPilotStart(totalOperations);
                    break;
                case IterationStage.Warmup when mode == IterationMode.Overhead:
                    OverheadWarmupStart(totalOperations);
                    break;
                case IterationStage.Warmup when mode == IterationMode.Workload:
                    WorkloadWarmupStart(totalOperations);
                    break;
                case IterationStage.Actual when mode == IterationMode.Overhead:
                    OverheadActualStart(totalOperations);
                    break;
                case IterationStage.Actual when mode == IterationMode.Workload:
                    WorkloadActualStart(totalOperations);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }

        [NonEvent]
        internal void IterationStop(IterationMode mode, IterationStage stage, long totalOperations)
        {
            switch (stage)
            {
                case IterationStage.Jitting when mode == IterationMode.Overhead:
                    OverheadJittingStop(totalOperations);
                    break;
                case IterationStage.Jitting when mode == IterationMode.Workload:
                    WorkloadJittingStop(totalOperations);
                    break;
                case IterationStage.Pilot when mode == IterationMode.Workload:
                    WorkloadPilotStop(totalOperations);
                    break;
                case IterationStage.Warmup when mode == IterationMode.Overhead:
                    OverheadWarmupStop(totalOperations);
                    break;
                case IterationStage.Warmup when mode == IterationMode.Workload:
                    WorkloadWarmupStop(totalOperations);
                    break;
                case IterationStage.Actual when mode == IterationMode.Overhead:
                    OverheadActualStop(totalOperations);
                    break;
                case IterationStage.Actual when mode == IterationMode.Workload:
                    WorkloadActualStop(totalOperations);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }
    }
}