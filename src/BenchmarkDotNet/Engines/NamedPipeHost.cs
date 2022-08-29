using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Validators;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using RuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Engines
{
    public class NamedPipeHost : IHost
    {
        internal const string NamedPipeArgument = "--namedPipe";
        internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly StreamWriter outWriter;
        private readonly StreamReader inReader;

        public NamedPipeHost(string namedPipeWritePath, string namedPipeReadPath)
        {
            outWriter = new StreamWriter(OpenNamedPipe(namedPipeWritePath, FileAccess.Write), UTF8NoBOM);
            // Flush the data to the Stream after each write, otherwise the server will wait for input endlessly!
            outWriter.AutoFlush = true;
            inReader = new StreamReader(OpenNamedPipe(namedPipeReadPath, FileAccess.Read), UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
        }

        private Stream OpenNamedPipe(string namedPipePath, FileAccess access)
        {
#if NETSTANDARD
            if (RuntimeInformation.IsWindows())
            {
                return WindowsSyscallCallHelper.OpenNamedPipe(namedPipePath, access);
            }
#endif

            FileShare share = RuntimeInformation.IsWindows() ? FileShare.None : FileShare.ReadWrite;
            return new FileStream(namedPipePath, FileMode.Open, access, share);
        }

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
            WriteLine(Engine.Signals.ToMessage(hostSignal));

            // read the response from Parent process, make the communication blocking
            string acknowledgment = inReader.ReadLine();
            if (acknowledgment != Engine.Signals.Acknowledgment)
                throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }

        public void SendError(string message) => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults) => runResults.Print(outWriter);

        [PublicAPI] // called from generated code
        public static bool TryGetNamedPipePath(string[] args, out string namedPipeWritePath, out string namedPipeReadPath)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == NamedPipeArgument)
                {
                    namedPipeWritePath = args[i + 1]; // IndexOutOfRangeException means a bug (incomplete data)
                    namedPipeReadPath = args[i + 2];
                    return true;
                }
            }

            namedPipeWritePath = namedPipeReadPath = null;
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

        internal static Stream CreateServer(string serverName, string filePath, PipeDirection pipeDirection)
        {
            if (RuntimeInformation.IsWindows())
            {
                return new NamedPipeServerStream(serverName, pipeDirection, maxNumberOfServerInstances: 1);
            }
            else
            {
                // mkfifo is not supported by WASM, but it's not a problem, as for WASM the host process is always .NET
                // and the benchmark process is actual WASM (it just opens the file using FileStream)
                int fd = mkfifo(filePath, 438); // 438 stands for 666 in octal
                if (fd == -1)
                {
                    throw new Exception("Unable to create named pipe");
                }

                return new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: 1);
            }

            [DllImport("libc", SetLastError = true)]
            static extern int mkfifo(string path, int mode);
        }
    }
}
