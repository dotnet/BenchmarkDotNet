using System.Collections.Generic;

namespace BenchmarkDotNet.Loggers
{
    public class LogCapture : ILogger
    {
        public IReadOnlyList<OutputLine> CapturedOutput => capturedOutput;

        private readonly List<OutputLine> capturedOutput = new List<OutputLine>(100);

        public string Id => nameof(LogCapture);
        public int Priority => 0;

        public void Write(LogKind logKind, string text)
        {
            capturedOutput.Add(new OutputLine
            {
                Kind = logKind,
                Text = text
            });
        }

        public void WriteLine()
        {
            capturedOutput.Add(new OutputLine
            {
                Kind = LogKind.Default,
                Text = System.Environment.NewLine
            });
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Write(logKind, text);
            WriteLine();
        }

        public void Flush() { }

        public void Clear() => capturedOutput.Clear();
    }

    public struct OutputLine
    {
        public LogKind Kind { get; set; }
        public string Text { get; set; }
    }
}
