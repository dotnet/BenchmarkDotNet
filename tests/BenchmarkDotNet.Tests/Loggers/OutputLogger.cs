using System;
using BenchmarkDotNet.Loggers;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Loggers
{
    public class OutputLogger : AccumulationLogger
    {
        private readonly ITestOutputHelper testOutputHelper;
        private string currentLine = "";

        public OutputLogger(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        }

        public override void Write(LogKind logKind, string text)
        {
            currentLine += RemoveInvalidChars(text);
            base.Write(logKind, text);
        }

        public override void WriteLine()
        {
            testOutputHelper.WriteLine(currentLine);
            currentLine = "";
            base.WriteLine();
        }

        public override void WriteLine(LogKind logKind, string text)
        {
            testOutputHelper.WriteLine(currentLine + RemoveInvalidChars(text));
            currentLine = "";
            base.WriteLine(logKind, text);
        }

        #region Xunit bug workaround

        /// <summary>
        /// Workaround for xunit bug: https://github.com/xunit/xunit/issues/876
        /// </summary>
        private static string RemoveInvalidChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text.Replace((char) 0x1B, ' ');
        }

        #endregion
    }
}