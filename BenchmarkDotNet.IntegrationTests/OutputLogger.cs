using System;
using BenchmarkDotNet.Loggers;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class OutputLogger : AccumulationLogger
    {
        private readonly ITestOutputHelper testOutputHelper;

        public OutputLogger(ITestOutputHelper testOutputHelper)
        {
            if (testOutputHelper == null)
                throw new ArgumentNullException(nameof(testOutputHelper));

            this.testOutputHelper = testOutputHelper;
        }

        public override void Write(LogKind logKind, string text)
        {
            testOutputHelper.WriteLine(RemoveInvalidChars(text));
            base.Write(logKind, text);
        }

        public override void WriteLine()
        {
            testOutputHelper.WriteLine(string.Empty);
            base.WriteLine();
        }

        public override void WriteLine(LogKind logKind, string text)
        {
            testOutputHelper.WriteLine(RemoveInvalidChars(text));
            base.WriteLine(logKind, text);
        }

        #region Xunit bug workaround
        /// <summary>
        /// Workaround for xunit bug: https://github.com/xunit/xunit/issues/876
        /// </summary>
        private static string RemoveInvalidChars(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace((char)0x1B, ' ');
        }
        #endregion
    }
}
