using System;
using System.IO;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public sealed class ConsoleHost : IHost
    {
        private readonly TextWriter outWriter;

        public ConsoleHost([NotNull] TextWriter outWriter, bool hasDiagnoserAttached)
        {
            if (outWriter == null)
                throw new ArgumentNullException(nameof(outWriter));
            this.outWriter = outWriter;
            IsDiagnoserAttached = hasDiagnoserAttached;
        }

        public bool IsDiagnoserAttached { get; }

        public void Write(string message) => outWriter.Write(message);

        public void WriteLine() => outWriter.WriteLine();

        public void WriteLine(string message) => outWriter.WriteLine(message);

        public void SendSignal(HostSignal hostSignal)
        {
            switch (hostSignal)
            {
                case HostSignal.BeforeAnythingElse:
                    WriteLine(Engine.Signals.BeforeAnythingElse);
                    break;
                case HostSignal.AfterSetup:
                    WriteLine(Engine.Signals.AfterSetup);
                    break;
                case HostSignal.BeforeMainRun:
                    WriteLine(Engine.Signals.BeforeMainRun);
                    break; ;
                case HostSignal.BeforeCleanup:
                    WriteLine(Engine.Signals.BeforeCleanup);
                    break;
                case HostSignal.AfterAnythingElse:
                    WriteLine(Engine.Signals.AfterAnythingElse);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hostSignal), hostSignal, null);
            }
        }

        public void ReportResults(RunResults runResults) => runResults.Print(outWriter);
    }
}
