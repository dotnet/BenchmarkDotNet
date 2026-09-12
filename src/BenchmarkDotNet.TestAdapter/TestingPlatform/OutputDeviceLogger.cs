using BenchmarkDotNet.Loggers;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;
using System.Text;
using System.Threading.Channels;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// Forwards the BenchmarkDotNet log to the platform output device, so that build progress and the result summary
    /// show up in the test run output.
    /// </summary>
    internal sealed class OutputDeviceLogger : ILogger
    {
        private readonly IOutputDevice outputDevice;
        private readonly IOutputDeviceDataProducer producer;
        private readonly ChannelWriter<Func<Task>> workQueue;
        private readonly CancellationToken cancellationToken;
        private readonly StringBuilder currentLine = new();
        private LogKind currentLineKind = LogKind.Default;

        public OutputDeviceLogger(
            IOutputDevice outputDevice,
            IOutputDeviceDataProducer producer,
            ChannelWriter<Func<Task>> workQueue,
            CancellationToken cancellationToken)
        {
            this.outputDevice = outputDevice;
            this.producer = producer;
            this.workQueue = workQueue;
            this.cancellationToken = cancellationToken;
        }

        public string Id => nameof(OutputDeviceLogger);

        public int Priority => 0;

        public void Write(LogKind logKind, string text)
        {
            currentLine.Append(text);

            // Assume that if any part of the line is an error or a warning, the whole line is.
            // The kind is reset when the line is flushed.
            if (logKind == LogKind.Error || (logKind == LogKind.Warning && currentLineKind != LogKind.Error))
                currentLineKind = logKind;
        }

        public void WriteLine()
        {
            var text = currentLine.ToString();
            var kind = currentLineKind;

            currentLine.Clear();
            currentLineKind = LogKind.Default;

            IOutputDeviceData data = kind switch
            {
                LogKind.Error => new ErrorMessageOutputDeviceData(text),
                LogKind.Warning => new WarningMessageOutputDeviceData(text),
                _ => new TextOutputDeviceData(text)
            };

            workQueue.TryWrite(() => outputDevice.DisplayAsync(producer, data, cancellationToken));
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Write(logKind, text);
            WriteLine();
        }

        public void Flush()
        {
            if (currentLine.Length > 0)
                WriteLine();
        }
    }
}
