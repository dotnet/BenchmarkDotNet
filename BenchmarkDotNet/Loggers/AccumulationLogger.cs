using System.Text;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Loggers
{
    public class AccumulationLogger : ILogger
    {
        private readonly StringBuilder builder = new StringBuilder();

        public virtual void Write(LogKind logKind, string format, params object[] args) =>
            builder.Append(string.Format(EnvironmentHelper.MainCultureInfo, format, args));

        public void ClearLog() => builder.Clear();
        public string GetLog() => builder.ToString();
    }
}