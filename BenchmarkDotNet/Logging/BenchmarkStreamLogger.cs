using System.Globalization;
using System.IO;

namespace BenchmarkDotNet.Logging
{
    public class BenchmarkStreamLogger : IBenchmarkLogger
    {
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