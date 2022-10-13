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
        private readonly ConcurrentQueue<string> output, error;
        private readonly bool logOutput, readStandardError;
        private readonly ILogger logger;

        private long status;

        internal AsyncProcessOutputReader(Process process, bool logOutput = false, ILogger logger = null, bool readStandardError = true)
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
            status = (long)Status.Created;
            this.logOutput = logOutput;
            this.logger = logger;
            this.readStandardError = readStandardError;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref status, (long)Status.Disposed);

            Detach();
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
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.Enqueue(e.Data);

                if (logOutput)
                {
                    lock (this) // #2125
                    {
                        logger.WriteLine(e.Data);
                    }
                }
            }
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error.Enqueue(e.Data);

                if (logOutput)
                {
                    lock (this) // #2125
                    {
                        logger.WriteLineError(e.Data);
                    }
                }
            }
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
