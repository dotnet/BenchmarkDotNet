using System;
using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Engines;
using Microsoft.Diagnostics.Tracing;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public sealed class IterationEvent : TraceEvent
    {
        public string JobId { get => GetUnicodeStringAt(0); }
        public string BenchmarkName { get => GetUnicodeStringAt(SkipUnicodeString(0)); }
        public IterationMode IterationMode { get => (IterationMode)GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); }
        public IterationStage IterationStage { get => (IterationStage)GetInt32At(SkipUnicodeString(SkipUnicodeString(0)) + sizeof(int)); }
        public long TotalOperations { get => GetInt64At(SkipUnicodeString(SkipUnicodeString(0)) + 2 * sizeof(int)); }
        
        private event Action<IterationEvent> target;

        internal IterationEvent(Action<IterationEvent> target, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.target = target;
        }

        protected override Delegate Target
        {
            get => target;
            set => target = (Action<IterationEvent>)value;
        }

        public override string[] PayloadNames
        {
            get => payloadNames ?? (payloadNames = new[] { nameof(JobId), nameof(BenchmarkName), nameof(IterationMode), nameof(IterationStage), nameof(TotalOperations) });
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(JobId), JobId);
            XmlAttrib(sb, nameof(BenchmarkName), BenchmarkName);
            XmlAttrib(sb, nameof(IterationMode), IterationMode);
            XmlAttrib(sb, nameof(IterationStage), IterationStage);
            XmlAttrib(sb, nameof(TotalOperations), TotalOperations);
            sb.Append("/>");
            
            return sb;
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return JobId;
                case 1:
                    return BenchmarkName;
                case 2:
                    return IterationMode;
                case 3:
                    return IterationStage;
                case 4:
                    return TotalOperations;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        protected override void Dispatch() => target?.Invoke(this);

        protected override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0)) + 12));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0)) + 12));
        }
    }
}