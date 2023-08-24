using System;
using System.IO;

namespace BenchmarkDotNet.Loggers
{
    public class TextLogger : ILogger, IDisposable
    {
        private readonly TextWriter writer;

        public TextLogger(TextWriter writer) => this.writer = writer;

        public virtual string Id => nameof(TextLogger);
        public int Priority => 0;

        public void Write(LogKind logKind, string text) => writer.Write(text);

        public void WriteLine() => writer.WriteLine();

        public void WriteLine(LogKind logKind, string text) => writer.WriteLine(text);

        public void Flush() => writer.Flush();

        public void Dispose() => writer.Dispose();
    }
}
