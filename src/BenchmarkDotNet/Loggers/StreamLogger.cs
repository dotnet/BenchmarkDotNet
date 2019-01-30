using System;
using System.IO;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public class StreamLogger : ILogger, IDisposable
    {
        private readonly StreamWriter writer;

        public StreamLogger(StreamWriter writer) => this.writer = writer;

        public void Dispose() => writer.Dispose();

        [PublicAPI]
        public StreamLogger(string filePath, bool append = false) => writer = new StreamWriter(filePath, append: false);

        public void Write(LogKind logKind, string text) => writer.Write(text);

        public void WriteLine() => writer.WriteLine();

        public void WriteLine(LogKind logKind, string text) => writer.WriteLine(text);

        public void Flush() => writer.Flush();
    }
}