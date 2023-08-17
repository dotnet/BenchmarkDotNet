using System;
using System.IO;

namespace BenchmarkDotNet.Loggers
{
    public class TextWriterLogger : ILogger, IDisposable
    {
        private readonly TextWriter writer;

        public TextWriterLogger(TextWriter writer) => this.writer = writer;

        public void Dispose() => writer.Dispose();

        public string Id => nameof(TextWriterLogger);
        public int Priority => 0;
        public void Write(LogKind logKind, string text) => writer.Write(text);

        public void WriteLine() => writer.WriteLine();

        public void WriteLine(LogKind logKind, string text) => writer.WriteLine(text);

        public void Flush() => writer.Flush();
    }
}
