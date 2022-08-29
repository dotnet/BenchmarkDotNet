using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Validators;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace BenchmarkDotNet.Engines
{
    public class NamedPipeHost : IHost
    {
        internal const string NamedPipeArgument = "--namedPipe";
        internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly Stream namedPipe;
        private readonly StreamWriter outWriter;
        private readonly StreamReader inReader;

        public NamedPipeHost(string namedPipePath)
        {
            namedPipe = OpenNamedPipe(namedPipePath);
            inReader = new StreamReader(namedPipe, UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            outWriter = new StreamWriter(namedPipe, UTF8NoBOM);
            // Flush the data to the Stream after each write, otherwise the server will wait for input endlessly!
            outWriter.AutoFlush = true;
        }

        private Stream OpenNamedPipe(string namedPipePath)
        {
            if (RuntimeInformation.IsWindows())
            {
#if NET6_0_OR_GREATER
                return new FileStream(namedPipePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
#else
                return WindowsSyscallCallHelper.OpenNamedPipe(namedPipePath);
#endif
            }
            else
            {
                NamedPipeClientStream client = new (namedPipePath);
                client.Connect(timeout: (int)TimeSpan.FromSeconds(10).TotalMilliseconds);
                return client;
            }
        }

        public void Dispose()
        {
            outWriter.Dispose();
            inReader.Dispose();
            namedPipe.Dispose();
        }

        public void Write(string message) => outWriter.Write(message);

        public void WriteLine() => outWriter.WriteLine();

        public void WriteLine(string message) => outWriter.WriteLine(message);

        public void SendSignal(HostSignal hostSignal)
        {
            WriteLine(Engine.Signals.ToMessage(hostSignal));

            // read the response from Parent process, make the communication blocking
            string acknowledgment = inReader.ReadLine();
            if (acknowledgment != Engine.Signals.Acknowledgment)
                throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }

        public void SendError(string message) => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults) => runResults.Print(outWriter);

        public static bool TryGetNamedPipePath(string[] args, out string namedPipePath)
        {
            // This method is invoked at the beginning of every benchmarking process.
            // We don't want to JIT any unnecessary .NET methods as this could affect their benchmarks
            // Example: Using LINQ here would cause some LINQ methods to be JITed before their first invocation,
            // which would make it impossible to measure their jitting time using Job.Dry for example.

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == NamedPipeArgument)
                {
                    namedPipePath = args[i + 1]; // IndexOutOfRangeException means a bug (incomplete data)
                    return true;
                }
            }

            namedPipePath = null;
            return false;
        }

        // logic based on https://github.com/dotnet/runtime/blob/a54a823ece1094dd05b7380614bd43566834a8f9/src/libraries/Common/tests/TestUtilities/System/IO/FileCleanupTestBase.cs#L154
        internal static (string serverName, string clientPath) GenerateNamedPipeNames()
        {
            string guid = Guid.NewGuid().ToString("N");
            if (RuntimeInformation.IsWindows())
            {
                return (guid, Path.GetFullPath($@"\\.\pipe\{guid}"));
            }

            return ($"/tmp/{guid}", $"/tmp/{guid}");
        }
    }
}
