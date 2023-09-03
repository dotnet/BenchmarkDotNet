using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BenchmarkDotNet.Loggers
{
    internal class AsyncProcessOutputReader : IDisposable
    {
        private readonly Process process;
        private readonly ILogger logger;
        private readonly bool logOutput, readStandardError;

        private static readonly TimeSpan FinishEventTimeout = TimeSpan.FromSeconds(1);
        private readonly AutoResetEvent outputFinishEvent, errorFinishEvent;
        private readonly ConcurrentQueue<string> output, error;

        private long status;

        internal AsyncProcessOutputReader(Process process, bool logOutput = false, ILogger? logger = null, bool readStandardError = true)
        {
            if (!process.StartInfo.RedirectStandardOutput)
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            if (readStandardError && !process.StartInfo.RedirectStandardError)
                throw new NotSupportedException("set RedirectStandardError to true first");
            if (logOutput && logger == null)
                throw new ArgumentException($"{nameof(logger)} cannot be null when {nameof(logOutput)} is true");

            this.process = process;
            output = new ConcurrentQueue<string>();
            error = new ConcurrentQueue<string>();
            outputFinishEvent = new AutoResetEvent(false);
            errorFinishEvent = new AutoResetEvent(false);
            status = (long)Status.Created;
            this.logOutput = logOutput;
            this.logger = logger;
            this.readStandardError = readStandardError;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref status, (long)Status.Disposed);

            Detach();

            outputFinishEvent.Dispose();
            errorFinishEvent.Dispose();
        }

        internal void BeginRead()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Started, (long)Status.Created) != (long)Status.Created)
                throw new InvalidOperationException("Reader can be started only once");

            Attach();

            process.BeginOutputReadLine();

            if (readStandardError)
                process.BeginErrorReadLine();
        }

        internal void CancelRead()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Stopped, (long)Status.Started) != (long)Status.Started)
                throw new InvalidOperationException("Only a started reader can be stopped");

            process.CancelOutputRead();

            if (readStandardError)
                process.CancelErrorRead();

            Detach();
        }

        internal void StopRead()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Stopped, (long)Status.Started) != (long)Status.Started)
                throw new InvalidOperationException("Only a started reader can be stopped");

            outputFinishEvent.WaitOne(FinishEventTimeout);
            if (readStandardError)
                errorFinishEvent.WaitOne(FinishEventTimeout);

            Detach();
        }

        internal ImmutableArray<string> GetOutputLines() => ReturnIfStopped(() => output.ToImmutableArray());

        internal ImmutableArray<string> GetErrorLines() => ReturnIfStopped(() => error.ToImmutableArray());

        internal ImmutableArray<string> GetOutputAndErrorLines() => ReturnIfStopped(() => output.Concat(error).ToImmutableArray());

        internal string GetOutputText() => ReturnIfStopped(() => string.Join(Environment.NewLine, output));

        internal string GetErrorText() => ReturnIfStopped(() => string.Join(Environment.NewLine, error));

        private void Attach()
        {
            process.OutputDataReceived += ProcessOnOutputDataReceived;

            if (readStandardError)
                process.ErrorDataReceived += ProcessOnErrorDataReceived;
        }

        private void Detach()
        {
            process.OutputDataReceived -= ProcessOnOutputDataReceived;

            if (readStandardError)
                process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Enqueue(e.Data);

                    if (logOutput)
                    {
                        logger.WriteLine(e.Data);
                    }
                }
            }
            else // 'e.Data == null' means EOF
                outputFinishEvent.Set();
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.Enqueue(e.Data);

                    if (logOutput)
                    {
                        logger.WriteLineError(e.Data);
                    }
                }
            }
            else // 'e.Data == null' means EOF
                errorFinishEvent.Set();
        }

        private T ReturnIfStopped<T>(Func<T> getter)
            => Interlocked.Read(ref status) == (long)Status.Stopped
                ? getter.Invoke()
                : throw new InvalidOperationException("The reader must be stopped first");

        private enum Status : long
        {
            Created,
            Started,
            Stopped,
            Disposed
        }
    }
}