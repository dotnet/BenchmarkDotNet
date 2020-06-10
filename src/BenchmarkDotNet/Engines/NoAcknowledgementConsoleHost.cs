using System;
using System.IO;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public sealed class NoAcknowledgementConsoleHost : IHost
    {
        private readonly TextWriter outWriter;

        public NoAcknowledgementConsoleHost([NotNull]TextWriter outWriter)
        {
            this.outWriter = outWriter ?? throw new ArgumentNullException(nameof(outWriter));
        }

        public void Write(string message) => outWriter.Write(message);

        public void WriteLine() => outWriter.WriteLine();

        public void WriteLine(string message) => outWriter.WriteLine(message);

        public void SendSignal(HostSignal hostSignal)
        {
            WriteLine(Engine.Signals.ToMessage(hostSignal));
        }

        public void SendError(string message) => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults) => runResults.Print(outWriter);
    }
}
