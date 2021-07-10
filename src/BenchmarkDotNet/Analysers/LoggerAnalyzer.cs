using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Analysers
{
    public class LoggerAnalyzer : IAnalyser, ILogger
    {
        private int errorCount;
        private int warningCount;
        public static readonly LoggerAnalyzer Instance = new LoggerAnalyzer();

        private LoggerAnalyzer()
        {

        }

        public string Id => nameof(LoggerAnalyzer);

        public int Priority => throw new NotImplementedException();

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            if (errorCount > 0)
            {
                yield return Conclusion.CreateError(Id, "There is one or more errors. See log for dettails.");
            }
            if (warningCount > 0)
            {
                yield return Conclusion.CreateWarning(Id, "There is one or more warning. See log for details.");
            }
        }

        public void Flush() { }

        public void Write(LogKind logKind, string text)
        {
            switch (logKind)
            {
                case LogKind.Error:
                    errorCount++;
                    break;
                case LogKind.Warning:
                    warningCount++;
                    break;
                default:
                    break;
            }
        }

        public void WriteLine() { }

        public void WriteLine(LogKind logKind, string text) => Write(logKind, text);
    }
}
