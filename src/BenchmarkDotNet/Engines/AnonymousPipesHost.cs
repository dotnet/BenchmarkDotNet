using BenchmarkDotNet.Validators;
using System;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public class AnonymousPipesHost : IHost
    {
        internal const string AnonymousPipesDescriptors = "--anonymousPipes";
        internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly StreamWriter outWriter;
        private readonly StreamReader inReader;

        public AnonymousPipesHost(string writHandle, string readHandle)
        {
            outWriter = new StreamWriter(OpenAnonymousPipe(writHandle, FileAccess.Write), UTF8NoBOM);
            // Flush the data to the Stream after each write, otherwise the host process will wait for input endlessly!
            outWriter.AutoFlush = true;
            inReader = new StreamReader(OpenAnonymousPipe(readHandle, FileAccess.Read), UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
        }

        private Stream OpenAnonymousPipe(string fileHandle, FileAccess access)
            => new FileStream(new SafeFileHandle(new IntPtr(int.Parse(fileHandle)), ownsHandle: true), access, bufferSize: 1);

        public void Dispose()
        {
            outWriter.Dispose();
            inReader.Dispose();
        }

        public void Write(string message) => outWriter.Write(message);

        public void WriteLine() => outWriter.WriteLine();

        public void WriteLine(string message) => outWriter.WriteLine(message);

        public void SendSignal(HostSignal hostSignal)
        {
            if (hostSignal == HostSignal.AfterAll)
            {
                // Before the last signal is reported and the benchmark process exits,
                // add an artificial sleep to increase the chance of host process reading all std output.
                System.Threading.Thread.Sleep(1);
            }

            WriteLine(Engine.Signals.ToMessage(hostSignal));

            // read the response from Parent process, make the communication blocking
            string acknowledgment = inReader.ReadLine();
            if (acknowledgment != Engine.Signals.Acknowledgment
                && !(acknowledgment is null && hostSignal == HostSignal.AfterAll)) // an early EOF, but still valid
            {
                throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
            }
        }

        public void SendError(string message) => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults) => runResults.Print(outWriter);

        [PublicAPI] // called from generated code
        public static bool TryGetFileHandles(string[] args, out string? writeHandle, out string? readHandle)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == AnonymousPipesDescriptors)
                {
                    writeHandle = args[i + 1]; // IndexOutOfRangeException means a bug (incomplete data)
                    readHandle = args[i + 2];
                    return true;
                }
            }

            writeHandle = readHandle = null;
            return false;
        }
    }
}
