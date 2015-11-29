using System.Globalization;
using System.Text;

namespace BenchmarkDotNet.Plugins.Loggers
{
    public class BenchmarkAccumulationLogger : IBenchmarkLogger
    {
        public string Name => "acc";
        public string Description => "Accumulation logger";

        private readonly StringBuilder builder = new StringBuilder();

        public void Write(BenchmarkLogKind logKind, string format, params object[] args)
        {
            builder.Append(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void ClearLog()
        {
            builder.Clear();
        }

        public string GetLog()
        {
            return builder.ToString();
        }
    }
}