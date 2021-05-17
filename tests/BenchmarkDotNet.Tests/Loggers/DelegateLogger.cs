using BenchmarkDotNet.Loggers;
using System;

namespace BenchmarkDotNet.Tests.Loggers
{
    internal class DelegateLogger : ILogger
    {
        private readonly Action<LogKind, string> writeAction;

        public DelegateLogger(Action<LogKind, string> writeAction)
        {
            this.writeAction = writeAction;
        }

        public string Id { get; }
        public int Priority => 0;

        public virtual void Write(LogKind logKind, string text)
            => writeAction?.Invoke(logKind, text);

        public virtual void WriteLine() { }

        public virtual void WriteLine(LogKind logKind, string text)
            => writeAction?.Invoke(logKind, string.Empty);

        public void Flush() { }
    }
}
