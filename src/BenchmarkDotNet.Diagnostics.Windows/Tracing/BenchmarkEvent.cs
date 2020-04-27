using System;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace BenchmarkDotNet.Diagnostics.Windows.Tracing
{
    public sealed class BenchmarkEvent : TraceEvent
    {
        public string BenchmarkName => GetUnicodeStringAt(0);

        private event Action<BenchmarkEvent> target;

        internal BenchmarkEvent(Action<BenchmarkEvent> target, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.target = target;
        }

        protected override Delegate Target
        {
            get => target;
            set => target = (Action<BenchmarkEvent>)value;
        }

        public override string[] PayloadNames => payloadNames ?? (payloadNames = new[] { nameof(BenchmarkName) });

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(BenchmarkName), BenchmarkName);
            sb.Append("/>");

            return sb;
        }

        public override object PayloadValue(int index) => index == 0 ? BenchmarkName : null;

        protected override void Dispatch() => target?.Invoke(this);
    }
}