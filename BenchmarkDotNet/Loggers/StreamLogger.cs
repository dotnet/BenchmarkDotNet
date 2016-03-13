using System.Globalization;
using System.IO;

namespace BenchmarkDotNet.Loggers
{
    public class StreamLogger : ILogger
    {
        private readonly StreamWriter writer;
        private readonly CultureInfo cultureInfo;

        public StreamLogger(StreamWriter writer, CultureInfo cultureInfo = null)
        {
            this.writer = writer;
            this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        public StreamLogger(string filePath, bool append = false)
        {
            writer = Portability.StreamWriter.FromPath(filePath, append);
        }

        public void Write(LogKind logKind, string format, params object[] args) =>
            writer.Write(args.Length == 0 ? format : string.Format(cultureInfo, format, args));
    }
}