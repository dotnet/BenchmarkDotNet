using System.Globalization;
using System.IO;

namespace BenchmarkDotNet.Plugins.Loggers
{
    public class BenchmarkStreamLogger : IBenchmarkLogger
    {
        public string Name => "stream";
        public string Description => "Stream logger";

        private readonly StreamWriter writer;
        private readonly CultureInfo cultureInfo;

        public BenchmarkStreamLogger(StreamWriter writer, CultureInfo cultureInfo = null)
        {
            this.writer = writer;
            this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        public BenchmarkStreamLogger(string filePath, bool append = false)
        {
            writer = new StreamWriter(filePath, append);
        }

        public void Write(BenchmarkLogKind logKind, string format, params object[] args)
        {
            if (args.Length == 0)
                writer.Write(format);
            else
                writer.Write(string.Format(cultureInfo, format, args));
        }
    }
}