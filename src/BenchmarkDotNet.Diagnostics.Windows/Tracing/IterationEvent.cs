using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace BenchmarkDotNet.Diagnostics.Windows.Tracing
{
    public sealed class IterationEvent : TraceEvent
    {
        public long TotalOperations => GetInt64At(0);

        private event Action<IterationEvent>? target;

        internal IterationEvent(Action<IterationEvent>? target, int eventId, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventId, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.target = target;
        }

        protected override Delegate? Target
        {
            get => target;
            set => target = (Action<IterationEvent>)value;
        }

        public override string[] PayloadNames => payloadNames ?? (payloadNames = new[] { nameof(TotalOperations) });

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(TotalOperations), TotalOperations);
            sb.Append("/>");

            return sb;
        }

        public override object? PayloadValue(int index) => index == 0 ? TotalOperations : null;

        protected override void Dispatch() => target?.Invoke(this);

        protected override void Validate() => Debug.Assert(!(Version == 0 && GetInt64At(0) == 0));
    }
}