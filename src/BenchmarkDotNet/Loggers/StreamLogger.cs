using System.IO;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public class StreamLogger : TextLogger
    {
        public StreamLogger(StreamWriter writer) : base(writer) { }

        [PublicAPI]
        public StreamLogger(string filePath, bool append = false)
            : this(new StreamWriter(filePath, append))
        { }

        public override string Id => nameof(StreamLogger);
    }
}
