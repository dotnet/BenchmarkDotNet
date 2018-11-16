﻿using System.IO;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public class StreamLogger : ILogger
    {
        private readonly StreamWriter writer;

        public StreamLogger(StreamWriter writer)
        {
            this.writer = writer;
        }

        [PublicAPI]
        public StreamLogger(string filePath, bool append = false)
        {
            writer = Portability.StreamWriter.FromPath(filePath, append);
        }

        public void Write(LogKind logKind, string text) => writer.Write(text);

        public void WriteLine() => writer.WriteLine();

        public void WriteLine(LogKind logKind, string text) => writer.WriteLine(text);

        public void Flush() => writer.Flush();
    }
}