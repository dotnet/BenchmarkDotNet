using System;
using System.IO;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public sealed class ConsoleHost : IHost
    {
        private readonly TextWriter outWriter;
        private readonly TextReader inReader;

        public ConsoleHost([NotNull]TextWriter outWriter, [NotNull]TextReader inReader)
        {
            this.outWriter = outWriter ?? throw new ArgumentNullException(nameof(outWriter));
            this.inReader = inReader ?? throw new ArgumentNullException(nameof(inReader));
        }

        public void Write(string message) => outWriter.Write(message);

        public void WriteLine() => outWriter.WriteLine();

        public void WriteLine(string message) => outWriter.WriteLine(message);

        public void SendSignal(HostSignal hostSignal)
        {
            WriteLine(Engine.Signals.ToMessage(hostSignal));

            // read the response from Parent process, make the communication blocking
            // I did not use Mutexes because they are not supported for Linux/MacOs for .NET Core
            // this solution is stupid simple and it works
            string acknowledgment = inReader.ReadLine();
            if (acknowledgment.IndexOf(Engine.Signals.Acknowledgment, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new NotSupportedException($"Unknown Acknowledgment: '{acknowledgment}'." + Environment.NewLine +
                    $"If for some reason you are running the benchmark process manually you just need to type '{Engine.Signals.Acknowledgment}' and hit enter.");
            }
        }

        public void SendError(string message) => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults) => runResults.Print(outWriter);
    }
}
