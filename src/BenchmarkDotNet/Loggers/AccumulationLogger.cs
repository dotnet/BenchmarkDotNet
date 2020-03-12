using System;
using System.Text;

namespace BenchmarkDotNet.Loggers
{
    public class AccumulationLogger : ILogger
    {
        private readonly StringBuilder builder = new StringBuilder();

        public AccumulationLogger()
        {
            // All AccumulationLoggers should have unique Ids
            Id = nameof(AccumulationLogger) + "." + Guid.NewGuid().ToString("N");
        }

        public string Id { get; }
        public int Priority => 0;

        public virtual void Write(LogKind logKind, string text) => builder.Append(text);

        public virtual void WriteLine() => builder.AppendLine();

        public virtual void WriteLine(LogKind logKind, string text) => builder.AppendLine(text);

        public void Flush() { }

        public void ClearLog() => builder.Clear();

        public string GetLog() => builder.ToString();
    }
}