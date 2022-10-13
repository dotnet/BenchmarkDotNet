using System;
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Write(LogKind logKind, string text)
        {
            currentLine += text;
            base.Write(logKind, text);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void WriteLine()
        {
            testOutputHelper.WriteLine(currentLine);
            currentLine = "";
            base.WriteLine();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void WriteLine(LogKind logKind, string text)
        {
            testOutputHelper.WriteLine(currentLine + text);
            currentLine = "";
            base.WriteLine(logKind, text);
        }
    }
}