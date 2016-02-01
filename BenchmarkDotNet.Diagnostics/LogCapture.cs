using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Diagnostics
{
    internal class LogCapture : ILogger
    {
        public IList<OutputLine> CapturedOutput = new List<OutputLine>();

        public void Write(LogKind logKind, string format, params object[] args)
        {
            CapturedOutput.Add(new OutputLine
            {
                Kind = logKind,
                Text = args.Length == 0 ? format : string.Format(CultureInfo.InvariantCulture, format, args)
            });
        }

        public void Clear()
        {
            CapturedOutput.Clear();
        }
    }

    internal class OutputLine
    {
        public LogKind Kind { get; set; }
        public string Text { get; set; }
    }
}
