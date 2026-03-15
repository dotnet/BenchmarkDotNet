using BenchmarkDotNet.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Loggers
{
    internal class AsyncProcessOutputReader : IDisposable
    {
        private readonly Process process;
        private readonly ILogger logger;
        private readonly bool logOutput, readStandardError;

        private static readonly TimeSpan FinishEventTimeout = TimeSpan.FromSeconds(1);
        private readonly TaskCompletionSource<object?> stdOutFinishTcs, errorFinishTcs;
        private readonly Channel<string>? outputChannel;
        private readonly ConcurrentQueue<string>? output;
        private readonly ConcurrentQueue<string> error;

        private long status;

        public Channel<string>? OutputChannel => outputChannel;

        internal AsyncProcessOutputReader(Process process, bool logOutput = false, ILogger? logger = null, bool readStandardError = true, bool cacheStandardOutput = true, bool channelStandardOutput = false)
        {
            if (!process.StartInfo.RedirectStandardOutput)
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            if (readStandardError && !process.StartInfo.RedirectStandardError)
                throw new NotSupportedException("set RedirectStandardError to true first");
            if (logOutput && logger == null)
                throw new ArgumentException($"{nameof(logger)} cannot be null when {nameof(logOutput)} is true");

            this.process = process;
            output = cacheStandardOutput ? new ConcurrentQueue<string>() : null;
            error = new ConcurrentQueue<string>();
            stdOutFinishTcs = new();
            errorFinishTcs = new();
            outputChannel = channelStandardOutput ? Channel.CreateUnbounded<string>() : null;
            status = (long)Status.Created;
            this.logOutput = logOutput;
            this.logger = logger ?? NullLogger.Instance;
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

        internal async ValueTask StopReadAsync()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Stopped, (long)Status.Started) != (long)Status.Started)
                throw new InvalidOperationException("Only a started reader can be stopped");

            await stdOutFinishTcs.Task.WaitAsync(FinishEventTimeout).ConfigureAwait(false);
            if (readStandardError)
                await errorFinishTcs.Task.WaitAsync(FinishEventTimeout).ConfigureAwait(false);

            Detach();
        }

        /// <summary>
        /// This must have been created with channelStandardOutput = true
        /// </summary>
        internal async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await OutputChannel!.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                return null;
            }
        }

        /// <summary>
        /// This must have been created with cacheStandardOutput = true
        /// </summary>
        internal ImmutableArray<string> GetOutputLines() => ReturnIfStopped(() => output!.ToImmutableArray());

        internal ImmutableArray<string> GetErrorLines() => ReturnIfStopped(() => error.ToImmutableArray());

        /// <summary>
        /// This must have been created with cacheStandardOutput = true
        /// </summary>
        internal ImmutableArray<string> GetOutputAndErrorLines() => ReturnIfStopped(() => output!.Concat(error).ToImmutableArray());

        /// <summary>
        /// This must have been created with cacheStandardOutput = true
        /// </summary>
        internal string GetOutputText() => ReturnIfStopped(() => string.Join(Environment.NewLine, output!));

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
                    output?.Enqueue(e.Data);
                    OutputChannel?.Writer.TryWrite(e.Data);

                    if (logOutput)
                    {
                        logger.WriteLine(e.Data);
                    }
                }
            }
            else // 'e.Data == null' means EOF
            {
                OutputChannel?.Writer.Complete();
                stdOutFinishTcs.SetResult(null);
            }
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
                errorFinishTcs.SetResult(null);
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