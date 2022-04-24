using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public sealed class WriteToFileHost : IHost
    {
        private readonly string filePath;
        private readonly StringBuilder output;

        public WriteToFileHost([NotNull] string filePath)
        {
            this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            output = new StringBuilder(3000);
        }

        public void Write(string message) => output.Append(message);

        public void WriteLine() => output.AppendLine();

        public void WriteLine(string message) => output.AppendLine(message);

        public void SendSignal(HostSignal hostSignal) => WriteLine(Engine.Signals.ToMessage(hostSignal));

        public void SendError(string message) => output.AppendLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults)
        {
            using StreamWriter writer = new (filePath);
            writer.Write(output.ToString());
            runResults.Print(writer);
        }
    }
}
